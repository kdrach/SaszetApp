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
        private readonly IVlmService _vlmService;

        public AdminProviderController(AppDbContext dbContext, ILlmProviderModelMapper mapper, IEncryptionService encryptionService, IVlmService vlmService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _encryptionService = encryptionService;
            _vlmService = vlmService;
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

                if (dto.ApiKey == "KEEP_EXISTING")
                {
                    return BadRequest("Cannot use KEEP_EXISTING for a new provider.");
                }

                var maxPriority = await _dbContext.LlmProviders
                    .Where(p => p.ProviderName == dto.ProviderName)
                    .MaxAsync(p => (int?)p.PriorityOrder) ?? 0;

                var entity = new LlmProviderEntity
                {
                    Id = Guid.NewGuid(),
                    ProviderName = dto.ProviderName,
                    ModelName = dto.ModelName,
                    EncryptedApiKey = _encryptionService.Encrypt(dto.ApiKey),
                    IsPrimary = dto.IsPrimary,
                    IsActive = dto.IsActive,
                    PriorityOrder = maxPriority + 1
                };
                _dbContext.LlmProviders.Add(entity);

                await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
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
            using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var entity = await _dbContext.LlmProviders.FindAsync(id);
                if (entity == null) return NotFound();

                if (!dto.IsPrimary && entity.IsPrimary)
                {
                    var primaryCount = await _dbContext.LlmProviders.CountAsync(p => p.IsPrimary);
                    if (primaryCount <= 1)
                    {
                        return BadRequest("Cannot unset the last primary provider.");
                    }
                }

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

                await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
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
            using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
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
                await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
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
        public async Task<IActionResult> TestConnection(Guid id, System.Threading.CancellationToken cancellationToken)
        {
            var provider = await _dbContext.LlmProviders.FindAsync(new object[] { id }, cancellationToken);
            if (provider == null) return NotFound();
            
            try
            {
                var decryptedKey = _encryptionService.Decrypt(provider.EncryptedApiKey);
                await _vlmService.TestConnectionAsync(provider.ProviderName, provider.ModelName, decryptedKey, cancellationToken);
                return Ok(new { status = "Connection tested successfully." });
            }
            catch (System.Exception)
            {
                return BadRequest(new { message = "Connection test failed." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProvider(Guid id)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var entity = await _dbContext.LlmProviders.FindAsync(id);
                if (entity == null) return NotFound();

                _dbContext.LlmProviders.Remove(entity);
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

        [HttpPut("{providerName}/reorder")]
        public async Task<IActionResult> ReorderProviders(string providerName, [FromBody] System.Collections.Generic.List<Guid> orderedIds)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var entities = await _dbContext.LlmProviders
                    .Where(p => p.ProviderName == providerName)
                    .ToListAsync();

                if (entities.Count != orderedIds.Count)
                {
                    return BadRequest(new { message = "The number of IDs provided does not match the number of keys for this provider." });
                }

                for (int i = 0; i < orderedIds.Count; i++)
                {
                    var entity = entities.FirstOrDefault(e => e.Id == orderedIds[i]);
                    if (entity == null)
                    {
                        return BadRequest(new { message = $"Provider key with ID {orderedIds[i]} not found for provider {providerName}." });
                    }
                    entity.PriorityOrder = i + 1;
                }

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
    }
}
