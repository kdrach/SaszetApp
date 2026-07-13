using System.Threading.Tasks;

namespace SaszetApp.Api.Services
{
    public interface IRateLimitingService
    {
        Task<bool> CheckLimitAsync(string userId);
        Task RecordUsageAsync(string userId);
    }
}
