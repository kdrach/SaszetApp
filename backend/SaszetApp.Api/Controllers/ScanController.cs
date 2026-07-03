using System.Linq;
using System.Threading.Tasks;
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
    [Microsoft.AspNetCore.Authorization.Authorize]
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
                .Where(p => p.Language == language && (p.EanCode == query || p.ProductName.ToLower().Contains(query.ToLower())))
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
    }
}
