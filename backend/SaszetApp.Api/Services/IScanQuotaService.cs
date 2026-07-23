using System.Threading;
using System.Threading.Tasks;
using SaszetApp.Api.Data;

using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services
{
    public interface IScanQuotaService
    {
        Task<UserScanUsageEntity?> CheckAndRecordUsageAsync(string userId, CancellationToken cancellationToken = default);
        Task RefundUsageAsync(UserScanUsageEntity entity, CancellationToken cancellationToken = default);
        Task<ScanQuotaStatus> GetQuotaStatusAsync(string userId, CancellationToken cancellationToken = default);
    }
}
