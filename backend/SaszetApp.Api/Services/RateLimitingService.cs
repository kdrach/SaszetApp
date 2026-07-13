using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;

namespace SaszetApp.Api.Services
{
    public class RateLimitingService : IRateLimitingService
    {
        private readonly AppDbContext _dbContext;

        public RateLimitingService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CheckLimitAsync(string userId)
        {
            int limit = 5;
            int rollingDays = 7;

            var globalLimitSetting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit");
            if (globalLimitSetting != null && int.TryParse(globalLimitSetting.Value, out int globalLimit))
            {
                limit = globalLimit;
            }

            var rollingDaysSetting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays");
            if (rollingDaysSetting != null && int.TryParse(rollingDaysSetting.Value, out int days))
            {
                rollingDays = days;
            }

            var userLimit = await _dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId);
            if (userLimit != null)
            {
                limit = userLimit.MaxScans;
            }

            var thresholdDate = DateTime.UtcNow.AddDays(-rollingDays);
            var usageCount = await _dbContext.UserScanUsages
                .Where(u => u.UserId == userId && u.ScannedAt >= thresholdDate)
                .CountAsync();

            return usageCount < limit;
        }

        public async Task RecordUsageAsync(string userId)
        {
            _dbContext.UserScanUsages.Add(new UserScanUsageEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ScannedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }
    }
}
