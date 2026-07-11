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
    [Authorize(Policy = "AdminPolicy")]
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
                    var currentPrimary = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.IsPrimary);
                    if (currentPrimary != null)
                    {
                        currentPrimary.IsPrimary = false;
                    }
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
                    if (dto.ApiKey == "KEEP_EXISTING")
                    {
                        return BadRequest("Cannot use KEEP_EXISTING for a new provider.");
                    }
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProvider(Guid id, [FromBody] CreateProviderDto dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var entity = await _dbContext.LlmProviders.FindAsync(id);
                if (entity == null) return NotFound();

                if (dto.IsPrimary && !entity.IsPrimary)
                {
                    var currentPrimary = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.IsPrimary);
                    if (currentPrimary != null)
                    {
                        currentPrimary.IsPrimary = false;
                    }
                }

                entity.ProviderName = dto.ProviderName;
                entity.ModelName = dto.ModelName;
                if (dto.ApiKey != "KEEP_EXISTING")
                {
                    entity.EncryptedApiKey = _encryptionService.Encrypt(dto.ApiKey);
                }
                entity.IsPrimary = dto.IsPrimary;
                entity.IsActive = dto.IsActive;

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

                var currentPrimary = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.IsPrimary);
                if (currentPrimary != null)
                {
                    currentPrimary.IsPrimary = false;
                }

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
                
                if (provider.ProviderName == "Anthropic")
                {
                    client.DefaultRequestHeaders.Add("x-api-key", decryptedKey);
                    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                    
                    var requestBody = new
                    {
                        model = provider.ModelName,
                        max_tokens = 1,
                        messages = new[] { new { role = "user", content = "Test" } }
                    };
                    var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return BadRequest(new { message = "Connection test failed.", details = error });
                    }
                }
                else if (provider.ProviderName == "Gemini")
                {
                    client.DefaultRequestHeaders.Add("x-goog-api-key", decryptedKey);
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{provider.ModelName}";
                    var response = await client.GetAsync(url);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return BadRequest(new { message = "Connection test failed.", details = error });
                    }
                }
                else
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", decryptedKey);
                    var url = $"https://api.openai.com/v1/models/{provider.ModelName}";
                    var response = await client.GetAsync(url);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return BadRequest(new { message = "Connection test failed.", details = error });
                    }
                }
                
                return Ok(new { status = "Connection tested successfully." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = "Connection test failed.", details = ex.Message });
            }
        }
    }
}
