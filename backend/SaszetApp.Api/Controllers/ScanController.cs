using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models;
using SaszetApp.Api.Services;
using SaszetApp.Api.Services.Mappers;
using SaszetApp.Api.DTOs;
using Microsoft.Extensions.Caching.Memory;
using SkiaSharp;

namespace SaszetApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "CustomerPolicy")]
    public class ScanController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IVlmService _vlmService;
        private readonly IPetFoodModelMapper _mapper;
        private readonly ILogger<ScanController> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly IScanQuotaService _scanQuotaService;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memoryCache;

        public ScanController(AppDbContext dbContext, IVlmService vlmService, IPetFoodModelMapper mapper, ILogger<ScanController> logger, IEncryptionService encryptionService, IScanQuotaService scanQuotaService, Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _vlmService = vlmService;
            _mapper = mapper;
            _logger = logger;
            _encryptionService = encryptionService;
            _scanQuotaService = scanQuotaService;
            _memoryCache = memoryCache;
        }

        [HttpGet("search")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ScanRatePolicy")]
        public async Task<IActionResult> Search([FromQuery] string query, System.Threading.CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                return BadRequest("Query must be at least 3 characters long.");
            }
            if (query.Length > 100) return BadRequest("Too long");

            var language = Request.Headers["Accept-Language"].ToString()?.Split(',').FirstOrDefault()?.Trim().ToLower() ?? "pl";
            if (!language.StartsWith("en") && !language.StartsWith("pl"))
            {
                language = "pl";
            }
            else if (language.StartsWith("en")) language = "en";
            else language = "pl";

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Cache lookup
            var cachedEntity = await _dbContext.PetFoodItems
                .Where(p => p.Language == language && (p.EanCode == query || p.ProductName.ToLower() == query.ToLower()) && p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cachedEntity != null)
            {
                return Ok(_mapper.MapToModel(cachedEntity));
            }

            // Fallback to VLM
            var usageEntity = await _scanQuotaService.CheckAndRecordUsageAsync(userId, cancellationToken);
            if (usageEntity == null)
            {
                return StatusCode(429, new { message = "You have reached your scan limit." });
            }

            PetFoodItemEntity newEntity;
            bool llmCallCompleted = false;
            try
            {
                if (!_memoryCache.TryGetValue("LlmFallbackChain", out System.Collections.Generic.List<LlmProviderEntity> fallbackChain))
                {
                    var activeCategory = await _dbContext.LlmProviders.Where(p => p.IsPrimary).Select(p => p.ProviderName).FirstOrDefaultAsync(cancellationToken);
                    if (activeCategory != null)
                    {
                        fallbackChain = await _dbContext.LlmProviders
                            .AsNoTracking()
                            .Where(p => p.ProviderName == activeCategory && p.IsActive)
                            .OrderBy(p => p.PriorityOrder)
                            .ToListAsync(cancellationToken);

                        _memoryCache.Set("LlmFallbackChain", fallbackChain, TimeSpan.FromMinutes(5));
                    }
                }

                if (fallbackChain == null || !fallbackChain.Any())
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                    _logger.LogWarning("No active keys for the selected LLM provider or provider is missing.");
                    return StatusCode(503, new { message = "No active keys or primary LLM provider configured." });
                }

                VlmResponseContract result = null;
                Exception lastException = null;

                foreach (var providerEntity in fallbackChain)
                {
                    var apiKey = _encryptionService.Decrypt(providerEntity.EncryptedApiKey);
                    try
                    {
                        result = await _vlmService.AnalyzeProductAsync(providerEntity.ProviderName, providerEntity.ModelName, apiKey, query, language, cancellationToken);
                        llmCallCompleted = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, $"Provider {providerEntity.ProviderName} (Model: {providerEntity.ModelName}) failed. Trying next backup...");
                    }
                }

                if (result == null)
                {
                    throw new Exception("All LLM providers in the fallback chain failed.", lastException);
                }

                newEntity = new PetFoodItemEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductName = result.ProductName,
                    Language = language,
                    Rating = result.Rating,
                    Pros = result.Pros,
                    Cons = result.Cons,
                    Summary = result.Summary,
                    ExtractedIngredients = result.ExtractedIngredients,
                    CreatedAt = DateTime.UtcNow
                };

                if (query.All(char.IsDigit) && query.Length >= 8)
                {
                    newEntity.EanCode = query;
                }

                _dbContext.PetFoodItems.Add(newEntity);
                await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);

                return Ok(_mapper.MapToModel(newEntity));
            }
            catch (Exception ex)
            {
                if (!llmCallCompleted)
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                }
                _logger.LogError(ex, "Error analyzing product.");
                return StatusCode(500, new { message = "Error analyzing product." });
            }
        }
        [HttpPost("analyze-image")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ScanRatePolicy")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 5242880)]
        public async Task<IActionResult> AnalyzeImage(IFormFile image, System.Threading.CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (image == null || image.Length == 0) return BadRequest("No image uploaded.");
            if (image.Length > 5 * 1024 * 1024) return BadRequest("Image size exceeds the 5MB limit.");

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(image.ContentType)) return BadRequest("Unsupported image format. Use JPEG, PNG, or WebP.");

            var language = Request.Headers["Accept-Language"].ToString()?.Split(',').FirstOrDefault()?.Trim().ToLower() ?? "pl";
            if (!language.StartsWith("en") && !language.StartsWith("pl")) language = "pl";
            else if (language.StartsWith("en")) language = "en";
            else language = "pl";

            var usageEntity = await _scanQuotaService.CheckAndRecordUsageAsync(userId, cancellationToken);
            if (usageEntity == null)
            {
                return StatusCode(429, new { message = "You have reached your scan limit." });
            }

            string base64Image;
            using (var memoryStream = new MemoryStream())
            {
                try
                {
                    using var inputStream = image.OpenReadStream();
                    using var codec = SKCodec.Create(inputStream);
                    if (codec == null) throw new Exception("Failed to decode image");
                    if (codec.Info.Width > 4096 || codec.Info.Height > 4096)
                    {
                        throw new Exception("Image dimensions too large.");
                    }
                    
                    inputStream.Position = 0;

                    await Task.Run(() =>
                    {
                        using var skBitmap = SKBitmap.Decode(inputStream);
                        if (skBitmap == null) throw new Exception("Failed to decode image");
                        
                        using var skImage = SKImage.FromBitmap(skBitmap);
                        using var data = skImage.Encode(SKEncodedImageFormat.Jpeg, 85); // EXIF is stripped
                        if (data != null)
                        {
                            data.SaveTo(memoryStream);
                        }
                        else
                        {
                            throw new Exception("Failed to encode image");
                        }
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, cancellationToken);
                    _logger.LogWarning(ex, "Failed to parse uploaded image. Possible malformed image attack.");
                    return BadRequest("Invalid image file.");
                }
                base64Image = Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            }

            PetFoodItemEntity newEntity;
            bool llmCallCompleted = false;
            try
            {
                if (!_memoryCache.TryGetValue("LlmFallbackChain", out System.Collections.Generic.List<LlmProviderEntity> fallbackChain))
                {
                    var activeCategory = await _dbContext.LlmProviders.Where(p => p.IsPrimary).Select(p => p.ProviderName).FirstOrDefaultAsync(cancellationToken);
                    if (activeCategory != null)
                    {
                        fallbackChain = await _dbContext.LlmProviders
                            .AsNoTracking()
                            .Where(p => p.ProviderName == activeCategory && p.IsActive)
                            .OrderBy(p => p.PriorityOrder)
                            .ToListAsync(cancellationToken);

                        _memoryCache.Set("LlmFallbackChain", fallbackChain, TimeSpan.FromMinutes(5));
                    }
                }

                if (fallbackChain == null || !fallbackChain.Any())
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                    _logger.LogWarning("No active keys for the selected LLM provider or provider is missing.");
                    return StatusCode(503, new { message = "No active keys or primary LLM provider configured." });
                }

                VlmResponseContract result = null;
                Exception lastException = null;

                foreach (var providerEntity in fallbackChain)
                {
                    var apiKey = _encryptionService.Decrypt(providerEntity.EncryptedApiKey);
                    try
                    {
                        result = await _vlmService.AnalyzeImageAsync(providerEntity.ProviderName, providerEntity.ModelName, apiKey, base64Image, "image/jpeg", language, cancellationToken);
                        llmCallCompleted = true;
                        break;
                    }
                    catch (InvalidOperationException ex) when (ex.Message == "NO_PET_FOOD_FOUND")
                    {
                        llmCallCompleted = true;
                        throw; // Don't fallback if it's a valid "not food" response
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, $"Provider {providerEntity.ProviderName} (Model: {providerEntity.ModelName}) failed for image scan. Trying next backup...");
                    }
                }

                if (result == null)
                {
                    throw new Exception("All LLM providers in the fallback chain failed.", lastException);
                }

                newEntity = new PetFoodItemEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductName = result.ProductName,
                    Language = language,
                    Rating = result.Rating,
                    Pros = result.Pros,
                    Cons = result.Cons,
                    Summary = result.Summary,
                    ExtractedIngredients = result.ExtractedIngredients,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.PetFoodItems.Add(newEntity);
                await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);

                return Ok(_mapper.MapToModel(newEntity));
            }
            catch (InvalidOperationException ex) when (ex.Message == "NO_PET_FOOD_FOUND")
            {
                if (!llmCallCompleted)
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                }
                return StatusCode(422, new { errorCode = "NO_PET_FOOD_FOUND" });
            }
            catch (Exception ex)
            {
                if (!llmCallCompleted)
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                }
                _logger.LogError(ex, "Error analyzing image.");
                return StatusCode(500, new { message = "Error analyzing image." });
            }
        }
        [HttpPost("compare")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ScanRatePolicy")]
        [RequestSizeLimit(25 * 1024 * 1024)] // 25MB total limit for up to 5 images
        [RequestFormLimits(MultipartBodyLengthLimit = 26214400)]
        public async Task<IActionResult> Compare([FromForm] System.Collections.Generic.List<IFormFile> images, System.Threading.CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (images == null || images.Count < 2 || images.Count > 5)
            {
                return BadRequest("Please provide between 2 and 5 images for comparison.");
            }

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            var base64Images = new System.Collections.Generic.List<string>();

            // Consume quota per comparison request
            var usageEntity = await _scanQuotaService.CheckAndRecordUsageAsync(userId, cancellationToken);
            if (usageEntity == null)
            {
                return StatusCode(429, new { message = "You have reached your scan limit." });
            }

            try
            {
                foreach (var image in images)
                {
                    if (image.Length == 0) throw new Exception("Empty image uploaded.");
                    if (!allowedMimeTypes.Contains(image.ContentType)) throw new Exception("Unsupported image format.");
                    
                    using var memoryStream = new MemoryStream();
                    using var inputStream = image.OpenReadStream();
                    using var codec = SKCodec.Create(inputStream);
                    if (codec == null) throw new Exception("Failed to decode image");
                    if (codec.Info.Width > 4096 || codec.Info.Height > 4096) throw new Exception("Image dimensions too large.");
                    
                    inputStream.Position = 0;
                    using var skBitmap = SKBitmap.Decode(inputStream);
                    if (skBitmap == null) throw new Exception("Failed to decode image");
                    
                    using var skImage = SKImage.FromBitmap(skBitmap);
                    using var data = skImage.Encode(SKEncodedImageFormat.Jpeg, 85);
                    if (data != null)
                    {
                        data.SaveTo(memoryStream);
                    }
                    else
                    {
                        throw new Exception("Failed to encode image");
                    }
                    base64Images.Add(Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length));
                }
            }
            catch (Exception ex)
            {
                await _scanQuotaService.RefundUsageAsync(usageEntity, cancellationToken);
                _logger.LogWarning(ex, "Failed to parse uploaded images.");
                return BadRequest("Invalid image file(s).");
            }

            var language = Request.Headers["Accept-Language"].ToString()?.Split(',').FirstOrDefault()?.Trim().ToLower() ?? "pl";
            if (!language.StartsWith("en") && !language.StartsWith("pl")) language = "pl";
            else if (language.StartsWith("en")) language = "en";
            else language = "pl";

            bool llmCallCompleted = false;
            try
            {
                if (!_memoryCache.TryGetValue("LlmFallbackChain", out System.Collections.Generic.List<LlmProviderEntity> fallbackChain))
                {
                    var activeCategory = await _dbContext.LlmProviders.Where(p => p.IsPrimary).Select(p => p.ProviderName).FirstOrDefaultAsync(cancellationToken);
                    if (activeCategory != null)
                    {
                        fallbackChain = await _dbContext.LlmProviders
                            .AsNoTracking()
                            .Where(p => p.ProviderName == activeCategory && p.IsActive)
                            .OrderBy(p => p.PriorityOrder)
                            .ToListAsync(cancellationToken);

                        _memoryCache.Set("LlmFallbackChain", fallbackChain, TimeSpan.FromMinutes(5));
                    }
                }

                if (fallbackChain == null || !fallbackChain.Any())
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                    return StatusCode(503, new { message = "No active keys or primary LLM provider configured." });
                }

                MultiVlmResponseContract result = null;
                Exception lastException = null;

                foreach (var providerEntity in fallbackChain)
                {
                    var apiKey = _encryptionService.Decrypt(providerEntity.EncryptedApiKey);
                    try
                    {
                        result = await _vlmService.AnalyzeMultipleImagesAsync(providerEntity.ProviderName, providerEntity.ModelName, apiKey, base64Images, "image/jpeg", language, cancellationToken);
                        llmCallCompleted = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, $"Provider {providerEntity.ProviderName} failed for comparison. Trying next...");
                    }
                }

                if (result == null)
                {
                    throw new Exception("All LLM providers in the fallback chain failed.", lastException);
                }

                var models = new System.Collections.Generic.List<PetFoodItem>();
                foreach (var prod in result.Products)
                {
                    var newEntity = new PetFoodItemEntity
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ProductName = prod.ProductName,
                        Language = language,
                        Rating = prod.Rating,
                        Pros = prod.Pros,
                        Cons = prod.Cons,
                        Summary = prod.Summary,
                        ExtractedIngredients = prod.ExtractedIngredients,
                        CreatedAt = DateTime.UtcNow
                    };
                    // Not caching the multi-comparison result as a block, but saving individual items is optional.
                    // For now, we return without inserting them to DB as per option A.
                    // Or we map directly to Model to return.
                    var model = new PetFoodItem
                    {
                        Id = newEntity.Id,
                        ProductName = newEntity.ProductName,
                        Language = newEntity.Language,
                        Rating = newEntity.Rating,
                        Pros = newEntity.Pros,
                        Cons = newEntity.Cons,
                        Summary = newEntity.Summary,
                        ExtractedIngredients = newEntity.ExtractedIngredients
                    };
                    models.Add(model);
                }

                return Ok(models);
            }
            catch (Exception ex)
            {
                if (!llmCallCompleted)
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                }
                _logger.LogError(ex, "Error comparing images.");
                return StatusCode(500, new { message = "Error comparing images." });
            }
        }
    }
}
