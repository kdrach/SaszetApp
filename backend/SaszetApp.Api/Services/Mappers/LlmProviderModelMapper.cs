using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services.Mappers
{
    public class LlmProviderModelMapper : ILlmProviderModelMapper
    {
        public LlmProvider MapToModel(LlmProviderEntity entity)
        {
            return new LlmProvider
            {
                Id = entity.Id,
                ProviderName = entity.ProviderName,
                ModelName = entity.ModelName,
                EncryptedApiKey = entity.EncryptedApiKey,
                IsPrimary = entity.IsPrimary,
                IsActive = entity.IsActive
            };
        }

        public LlmProviderEntity MapToEntity(LlmProvider model)
        {
            return new LlmProviderEntity
            {
                Id = model.Id,
                ProviderName = model.ProviderName,
                ModelName = model.ModelName,
                EncryptedApiKey = model.EncryptedApiKey,
                IsPrimary = model.IsPrimary,
                IsActive = model.IsActive
            };
        }
    }
}
