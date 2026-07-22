using System.Threading;
using System.Threading.Tasks;
using SaszetApp.Api.Data;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services
{
    public interface IVlmService
    {
        Task<VlmResponseContract> AnalyzeProductAsync(string providerName, string modelName, string apiKey, string query, string language, CancellationToken cancellationToken);
        Task<VlmResponseContract> AnalyzeImageAsync(string providerName, string modelName, string apiKey, string base64Image, string mimeType, string language, CancellationToken cancellationToken);
        Task<MultiVlmResponseContract> AnalyzeMultipleImagesAsync(string providerName, string modelName, string apiKey, System.Collections.Generic.List<string> base64Images, string mimeType, string language, CancellationToken cancellationToken);
        Task<VlmResponseContract> PersonalizeAnalysisAsync(string providerName, string modelName, string apiKey, VlmResponseContract genericResult, string userProfileContext, string language, CancellationToken cancellationToken);
        Task TestConnectionAsync(string providerName, string modelName, string apiKey, CancellationToken cancellationToken);
    }
}
