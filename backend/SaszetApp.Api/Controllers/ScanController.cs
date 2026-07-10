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

        public ScanController(AppDbContext dbContext, IVlmService vlmService, IPetFoodModelMapper mapper)
        {
            _dbContext = dbContext;
            _vlmService = vlmService;
            _mapper = mapper;
        }

        [HttpGet("search")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ScanRatePolicy")]
        public async Task<IActionResult> Search([FromQuery] string query)
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

            // Cache lookup
            var cachedEntity = await _dbContext.PetFoodItems
                .Where(p => p.Language == language && (p.EanCode == query || p.ProductName.ToLower() == query.ToLower()))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (cachedEntity != null)
            {
                return Ok(_mapper.MapToModel(cachedEntity));
            }

            // Fallback to VLM
            try
            {
                var result = await _vlmService.AnalyzeProductAsync(query, language);
                return Ok(result);
            }
            catch (System.Exception)
            {
                return StatusCode(500, new { message = "Error analyzing product." });
            }
        }
        [HttpPost("analyze-image")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ScanRatePolicy")]
        public async Task<IActionResult> AnalyzeImage(IFormFile image, [FromForm] ScanMode mode)
        {
            if (image == null || image.Length == 0) return BadRequest("No image uploaded.");
            if (image.Length > 5 * 1024 * 1024) return BadRequest("Image size exceeds the 5MB limit.");

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(image.ContentType)) return BadRequest("Unsupported image format. Use JPEG, PNG, or WebP.");

            var language = Request.Headers["Accept-Language"].ToString()?.Split(',').FirstOrDefault()?.Trim().ToLower() ?? "pl";
            if (!language.StartsWith("en") && !language.StartsWith("pl")) language = "pl";
            else if (language.StartsWith("en")) language = "en";
            else language = "pl";

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var base64Image = Convert.ToBase64String(memoryStream.ToArray());

            try
            {
                var result = await _vlmService.AnalyzeImageAsync(base64Image, image.ContentType, mode, language);
                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "NO_PET_FOOD_FOUND")
            {
                return StatusCode(422, new { errorCode = "NO_PET_FOOD_FOUND" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error analyzing image." });
            }
        }
    }
}
