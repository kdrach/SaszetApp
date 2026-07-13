using System.Threading;
using System.Threading.Tasks;
using SaszetApp.Api.Data;

namespace SaszetApp.Api.Services
{
    public interface IScanQuotaService
    {
        Task<bool> CheckLimitAsync(string userId, CancellationToken cancellationToken = default);
        UserScanUsageEntity RecordUsage(string userId);
    }
}
