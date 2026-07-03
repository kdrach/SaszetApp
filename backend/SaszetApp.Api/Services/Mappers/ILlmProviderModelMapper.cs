using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services.Mappers
{
    public interface ILlmProviderModelMapper
    {
        LlmProvider MapToModel(LlmProviderEntity entity);
        LlmProviderEntity MapToEntity(LlmProvider model);
    }
}
