using System.Threading;
using System.Threading.Tasks;
using SaszetApp.Api.Data;

namespace SaszetApp.Api.Services
{
    public interface IScanQuotaService
    {
        Task<UserScanUsageEntity?> CheckAndRecordUsageAsync(string userId, CancellationToken cancellationToken = default);
        Task RefundUsageAsync(UserScanUsageEntity entity, CancellationToken cancellationToken = default);
        Task<(int Remaining, int Limit)> GetQuotaStatusAsync(string userId, CancellationToken cancellationToken = default);
    }
}
