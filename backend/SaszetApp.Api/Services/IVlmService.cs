using System.Threading.Tasks;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services
{
    public interface IVlmService
    {
        Task<PetFoodItem> AnalyzeProductAsync(string query, string language);
        Task<PetFoodItem> AnalyzeImageAsync(string base64Image, string mimeType, ScanMode mode, string language);
        Task TestConnectionAsync(LlmProviderEntity provider, System.Threading.CancellationToken cancellationToken);
    }
}
