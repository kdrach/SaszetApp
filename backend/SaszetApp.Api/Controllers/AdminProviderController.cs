using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models;
using SaszetApp.Api.Services;
using SaszetApp.Api.Services.Mappers;
using System.ComponentModel.DataAnnotations;

namespace SaszetApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminProviderController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILlmProviderModelMapper _mapper;
        private readonly IEncryptionService _encryptionService;
        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;

        public AdminProviderController(AppDbContext dbContext, ILlmProviderModelMapper mapper, IEncryptionService encryptionService, System.Net.Http.IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _encryptionService = encryptionService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetProviders()
        {
            var entities = await _dbContext.LlmProviders.ToListAsync();
            // Don't expose API keys
            var models = entities.Select(e => {
                var m = _mapper.MapToModel(e);
                m.EncryptedApiKey = "********";
                return m;
            });
            return Ok(models);
        }

        public class CreateProviderDto
        {
            [Required]
            [RegularExpression("^(OpenAI|Anthropic|Gemini)$")]
            public string ProviderName { get; set; } = string.Empty;
            [Required]
            public string ModelName { get; set; } = string.Empty;
            [Required]
            public string ApiKey { get; set; } = string.Empty;
            public bool IsPrimary { get; set; }
            public bool IsActive { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProvider([FromBody] CreateProviderDto dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (dto.IsPrimary)
                {
                    await _dbContext.LlmProviders.Where(p => p.IsPrimary).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsPrimary, false));
                }

                var entity = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.ProviderName == dto.ProviderName);
                if (entity != null)
                {
                    entity.ModelName = dto.ModelName;
                    if (dto.ApiKey != "KEEP_EXISTING")
                    {
                        entity.EncryptedApiKey = _encryptionService.Encrypt(dto.ApiKey);
                    }
                    entity.IsPrimary = dto.IsPrimary;
                    entity.IsActive = dto.IsActive;
                }
                else
                {
                    entity = new LlmProviderEntity
                    {
                        Id = Guid.NewGuid(),
                        ProviderName = dto.ProviderName,
                        ModelName = dto.ModelName,
                        EncryptedApiKey = _encryptionService.Encrypt(dto.ApiKey),
                        IsPrimary = dto.IsPrimary,
                        IsActive = dto.IsActive
                    };
                    _dbContext.LlmProviders.Add(entity);
                }
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var model = _mapper.MapToModel(entity);
                model.EncryptedApiKey = "********";
                return Ok(model);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPut("{id}/set-primary")]
        public async Task<IActionResult> SetPrimary(Guid id)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var provider = await _dbContext.LlmProviders.FindAsync(id);
                if (provider == null) return NotFound();

                await _dbContext.LlmProviders.Where(p => p.IsPrimary).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsPrimary, false));

                provider.IsPrimary = true;
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPost("{id}/test")]
        public async Task<IActionResult> TestConnection(Guid id)
        {
            var provider = await _dbContext.LlmProviders.FindAsync(id);
            if (provider == null) return NotFound();
            
            var decryptedKey = _encryptionService.Decrypt(provider.EncryptedApiKey);
            if (string.IsNullOrWhiteSpace(decryptedKey)) 
                return BadRequest("Invalid or missing API key.");
                
            try
            {
                using var client = _httpClientFactory.CreateClient();
                
                string url = provider.ProviderName switch
                {
                    "Anthropic" => "https://api.anthropic.com/v1/models",
                    "Gemini" => $"https://generativelanguage.googleapis.com/v1beta/models?key={decryptedKey}",
                    _ => "https://api.openai.com/v1/models"
                };

                if (provider.ProviderName == "Anthropic")
                {
                    client.DefaultRequestHeaders.Add("x-api-key", decryptedKey);
                    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                }
                else if (provider.ProviderName != "Gemini")
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", decryptedKey);
                }
                
                // Minimal validation/ping
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return Ok(new { status = "Connection tested successfully." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Connection test failed.", details = ex.Message });
            }
        }
    }
}
