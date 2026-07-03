using System.Threading.Tasks;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services
{
    public interface IVlmService
    {
        Task<PetFoodItem> AnalyzeProductAsync(string query, string language);
    }
}
