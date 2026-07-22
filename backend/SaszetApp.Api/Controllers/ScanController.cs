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
using Microsoft.Extensions.Logging;

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

        private async Task<System.Collections.Generic.List<LlmProviderEntity>?> GetFallbackChainAsync(System.Threading.CancellationToken cancellationToken)
        {
            if (!_memoryCache.TryGetValue("LlmFallbackChain", out System.Collections.Generic.List<LlmProviderEntity>? fallbackChain))
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
            return fallbackChain;
        }

        private async Task<string?> GetUserProfileContextAsync(string userId, System.Threading.CancellationToken cancellationToken)
        {
            var cats = await _dbContext.Cats.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
            if (!cats.Any()) return null;
            var catProfiles = cats.Select(c => $"Name: {c.Name}, Allergies: {(c.Allergies != null && c.Allergies.Any() ? string.Join(", ", c.Allergies) : "None")}").ToList();
            return $"Cats: {string.Join("; ", catProfiles)}";
        }

        private async Task<VlmResponseContract?> PersonalizeAsync(VlmResponseContract? genericResult, string? userProfileContext, string language, System.Collections.Generic.List<LlmProviderEntity>? fallbackChain, System.Threading.CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userProfileContext) || fallbackChain == null || !fallbackChain.Any() || genericResult == null)
                return genericResult;

            VlmResponseContract? personalizedResult = null;
            foreach (var providerEntity in fallbackChain)
            {
                var apiKey = _encryptionService.Decrypt(providerEntity.EncryptedApiKey);
                try
                {
                    personalizedResult = await _vlmService.PersonalizeAnalysisAsync(providerEntity.ProviderName, providerEntity.ModelName, apiKey, genericResult, userProfileContext, language, cancellationToken);
                    if (personalizedResult != null) break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Provider {providerEntity.ProviderName} failed to personalize. Trying next backup...");
                }
            }
            return personalizedResult ?? genericResult;
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
            if (!language.StartsWith("en") && !language.StartsWith("pl")) language = "pl";
            else if (language.StartsWith("en")) language = "en";
            else language = "pl";

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var fallbackChain = await GetFallbackChainAsync(cancellationToken);
            var userProfileContext = await GetUserProfileContextAsync(userId, cancellationToken);

            var cachedEntity = await _dbContext.PetFoodItems
                .Where(p => p.Language == language && (p.EanCode == query || p.ProductName.ToLower() == query.ToLower()) && p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            VlmResponseContract? genericResult = null;
            Guid? entityId = cachedEntity?.Id;
            string? entityEanCode = cachedEntity?.EanCode;

            bool chargedForPersonalizationOnly = false;
            UserScanUsageEntity? usageEntity = null;

            if (cachedEntity != null)
            {
                genericResult = new VlmResponseContract 
                {
                    ProductName = cachedEntity.ProductName,
                    Rating = cachedEntity.Rating,
                    Pros = cachedEntity.Pros,
                    Cons = cachedEntity.Cons,
                    Summary = cachedEntity.Summary,
                    ExtractedIngredients = cachedEntity.ExtractedIngredients
                };
                
                if (!string.IsNullOrEmpty(userProfileContext) && fallbackChain != null && fallbackChain.Any())
                {
                    usageEntity = await _scanQuotaService.CheckAndRecordUsageAsync(userId, cancellationToken);
                    if (usageEntity == null)
                    {
                        return StatusCode(429, new { message = "You have reached your scan limit." });
                    }
                    chargedForPersonalizationOnly = true;
                }
            }
            else
            {
                usageEntity = await _scanQuotaService.CheckAndRecordUsageAsync(userId, cancellationToken);
                if (usageEntity == null)
                {
                    return StatusCode(429, new { message = "You have reached your scan limit." });
                }

                if (fallbackChain == null || !fallbackChain.Any())
                {
                    await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                    _logger.LogWarning("No active keys for the selected LLM provider or provider is missing.");
                    return StatusCode(503, new { message = "No active keys or primary LLM provider configured." });
                }

                bool llmCallCompleted = false;
                Exception? lastException = null;

                try
                {
                    foreach (var providerEntity in fallbackChain)
                    {
                        var apiKey = _encryptionService.Decrypt(providerEntity.EncryptedApiKey);
                        try
                        {
                            genericResult = await _vlmService.AnalyzeProductAsync(providerEntity.ProviderName, providerEntity.ModelName, apiKey, query, language, cancellationToken);
                            llmCallCompleted = true;
                            break;
                        }
                        catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode >= System.Net.HttpStatusCode.BadRequest && ex.StatusCode < System.Net.HttpStatusCode.InternalServerError)
                        {
                            llmCallCompleted = true;
                            throw;
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                            _logger.LogWarning(ex, $"Provider {providerEntity.ProviderName} (Model: {providerEntity.ModelName}) failed. Trying next backup...");
                        }
                    }

                    if (genericResult == null)
                    {
                        throw new Exception("All LLM providers in the fallback chain failed.", lastException);
                    }

                    var newEntity = new PetFoodItemEntity
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ProductName = genericResult.ProductName,
                        Language = language,
                        Rating = genericResult.Rating,
                        Pros = genericResult.Pros,
                        Cons = genericResult.Cons,
                        Summary = genericResult.Summary,
                        ExtractedIngredients = genericResult.ExtractedIngredients,
                        CreatedAt = DateTime.UtcNow
                    };

                    if (query.All(char.IsDigit) && query.Length >= 8)
                    {
                        newEntity.EanCode = query;
                    }

                    _dbContext.PetFoodItems.Add(newEntity);
                    await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);

                    entityId = newEntity.Id;
                    entityEanCode = newEntity.EanCode;
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

            var finalResult = await PersonalizeAsync(genericResult, userProfileContext, language, fallbackChain, cancellationToken);

            if (chargedForPersonalizationOnly && Object.ReferenceEquals(finalResult, genericResult))
            {
                await _scanQuotaService.RefundUsageAsync(usageEntity!, System.Threading.CancellationToken.None);
            }

            var model = new PetFoodItem
            {
                Id = entityId ?? Guid.NewGuid(),
                ProductName = finalResult?.ProductName ?? string.Empty,
                Language = language,
                Rating = finalResult?.Rating ?? 0,
                Pros = finalResult?.Pros ?? new System.Collections.Generic.List<string>(),
                Cons = finalResult?.Cons ?? new System.Collections.Generic.List<string>(),
                Summary = finalResult?.Summary ?? string.Empty,
                ExtractedIngredients = finalResult?.ExtractedIngredients ?? string.Empty,
                EanCode = entityEanCode
            };

            return Ok(model);
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

            var fallbackChain = await GetFallbackChainAsync(cancellationToken);
            var userProfileContext = await GetUserProfileContextAsync(userId, cancellationToken);

            var usageEntity = await _scanQuotaService.CheckAndRecordUsageAsync(userId, cancellationToken);
            if (usageEntity == null)
            {
                return StatusCode(429, new { message = "You have reached your scan limit." });
            }

            if (fallbackChain == null || !fallbackChain.Any())
            {
                await _scanQuotaService.RefundUsageAsync(usageEntity, System.Threading.CancellationToken.None);
                _logger.LogWarning("No active keys for the selected LLM provider or provider is missing.");
                return StatusCode(503, new { message = "No active keys or primary LLM provider configured." });
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
                        using var data = skImage.Encode(SKEncodedImageFormat.Jpeg, 85);
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

            VlmResponseContract? genericResult = null;
            bool llmCallCompleted = false;
            try
            {
                Exception? lastException = null;

                foreach (var providerEntity in fallbackChain)
                {
                    var apiKey = _encryptionService.Decrypt(providerEntity.EncryptedApiKey);
                    try
                    {
                        genericResult = await _vlmService.AnalyzeImageAsync(providerEntity.ProviderName, providerEntity.ModelName, apiKey, base64Image, "image/jpeg", language, cancellationToken);
                        llmCallCompleted = true;
                        break;
                    }
                    catch (InvalidOperationException ex) when (ex.Message == "NO_PET_FOOD_FOUND")
                    {
                        llmCallCompleted = true;
                        throw;
                    }
                    catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode >= System.Net.HttpStatusCode.BadRequest && ex.StatusCode < System.Net.HttpStatusCode.InternalServerError)
                    {
                        llmCallCompleted = true;
                        throw;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, $"Provider {providerEntity.ProviderName} (Model: {providerEntity.ModelName}) failed for image scan. Trying next backup...");
                    }
                }

                if (genericResult == null)
                {
                    throw new Exception("All LLM providers in the fallback chain failed.", lastException);
                }

                var newEntity = new PetFoodItemEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProductName = genericResult.ProductName,
                    Language = language,
                    Rating = genericResult.Rating,
                    Pros = genericResult.Pros,
                    Cons = genericResult.Cons,
                    Summary = genericResult.Summary,
                    ExtractedIngredients = genericResult.ExtractedIngredients,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.PetFoodItems.Add(newEntity);
                await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);

                var finalResult = await PersonalizeAsync(genericResult, userProfileContext, language, fallbackChain, cancellationToken);

                var model = new PetFoodItem
                {
                    Id = newEntity.Id,
                    ProductName = finalResult?.ProductName ?? string.Empty,
                    Language = language,
                    Rating = finalResult?.Rating ?? 0,
                    Pros = finalResult?.Pros ?? new System.Collections.Generic.List<string>(),
                    Cons = finalResult?.Cons ?? new System.Collections.Generic.List<string>(),
                    Summary = finalResult?.Summary ?? string.Empty,
                    ExtractedIngredients = finalResult?.ExtractedIngredients ?? string.Empty
                };

                return Ok(model);
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
        [RequestSizeLimit(25 * 1024 * 1024)]
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

            var language = Request.Headers["Accept-Language"].ToString()?.Split(',').FirstOrDefault()?.Trim().ToLower() ?? "pl";
            if (!language.StartsWith("en") && !language.StartsWith("pl")) language = "pl";
            else if (language.StartsWith("en")) language = "en";
            else language = "pl";

            var fallbackChain = await GetFallbackChainAsync(cancellationToken);
            var userProfileContext = await GetUserProfileContextAsync(userId, cancellationToken);

            var usageEntities = new System.Collections.Generic.List<UserScanUsageEntity>();
            for (int i = 0; i < images.Count; i++)
            {
                var usage = await _scanQuotaService.CheckAndRecordUsageAsync(userId, cancellationToken);
                if (usage == null)
                {
                    foreach (var u in usageEntities)
                    {
                        await _scanQuotaService.RefundUsageAsync(u, cancellationToken);
                    }
                    return StatusCode(429, new { message = "You have reached your scan limit." });
                }
                usageEntities.Add(usage);
            }

            if (fallbackChain == null || !fallbackChain.Any())
            {
                foreach (var u in usageEntities)
                {
                    await _scanQuotaService.RefundUsageAsync(u, System.Threading.CancellationToken.None);
                }
                return StatusCode(503, new { message = "No active keys or primary LLM provider configured." });
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
                    if (codec.Info.Width > 2048 || codec.Info.Height > 2048) throw new Exception("Image dimensions too large.");
                    
                    inputStream.Position = 0;
                    
                    await Task.Run(() =>
                    {
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
                    }, cancellationToken);

                    base64Images.Add(Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length));
                }
            }
            catch (Exception ex)
            {
                foreach (var u in usageEntities)
                {
                    await _scanQuotaService.RefundUsageAsync(u, cancellationToken);
                }
                _logger.LogWarning(ex, "Failed to parse uploaded images.");
                return BadRequest("Invalid image file(s).");
            }

            bool llmCallCompleted = false;
            try
            {
                MultiVlmResponseContract? result = null;
                Exception? lastException = null;

                foreach (var providerEntity in fallbackChain)
                {
                    var apiKey = _encryptionService.Decrypt(providerEntity.EncryptedApiKey);
                    try
                    {
                        result = await _vlmService.AnalyzeMultipleImagesAsync(providerEntity.ProviderName, providerEntity.ModelName, apiKey, base64Images, "image/jpeg", language, cancellationToken);
                        llmCallCompleted = true;
                        break;
                    }
                    catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode >= System.Net.HttpStatusCode.BadRequest && ex.StatusCode < System.Net.HttpStatusCode.InternalServerError)
                    {
                        llmCallCompleted = true;
                        lastException = ex;
                        _logger.LogWarning(ex, $"Provider {providerEntity.ProviderName} failed with 4xx for comparison.");
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
                if (result.Products != null)
                {
                    foreach (var prod in result.Products)
                    {
                        if (prod == null || string.IsNullOrWhiteSpace(prod.ProductName))
                            continue;

                        var finalProd = await PersonalizeAsync(prod, userProfileContext, language, fallbackChain, cancellationToken);

                        var model = new PetFoodItem
                        {
                            Id = Guid.NewGuid(),
                            ProductName = finalProd?.ProductName ?? string.Empty,
                            Language = language,
                            Rating = finalProd?.Rating ?? 0,
                            Pros = finalProd?.Pros ?? new System.Collections.Generic.List<string>(),
                            Cons = finalProd?.Cons ?? new System.Collections.Generic.List<string>(),
                            Summary = finalProd?.Summary ?? string.Empty,
                            ExtractedIngredients = finalProd?.ExtractedIngredients ?? string.Empty
                        };
                        models.Add(model);
                    }
                }

                return Ok(models);
            }
            catch (Exception ex)
            {
                if (!llmCallCompleted)
                {
                    foreach (var u in usageEntities)
                    {
                        await _scanQuotaService.RefundUsageAsync(u, System.Threading.CancellationToken.None);
                    }
                }
                _logger.LogError(ex, "Error comparing images.");
                return StatusCode(500, new { message = "Error comparing images." });
            }
        }
    }
}
