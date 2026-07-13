using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SaszetApp.Api.Data;

namespace SaszetApp.Api.Services
{
    public class ScanQuotaService : IScanQuotaService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _cache;

        public ScanQuotaService(AppDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<UserScanUsageEntity?> CheckAndRecordUsageAsync(string userId, CancellationToken cancellationToken = default)
        {
            int limit = 5;
            int rollingDays = 7;

            if (!_cache.TryGetValue("GlobalScanLimit", out int globalLimit))
            {
                var globalLimitSetting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit", cancellationToken);
                if (globalLimitSetting != null && int.TryParse(globalLimitSetting.Value, out int parsedLimit))
                {
                    globalLimit = parsedLimit;
                }
                else
                {
                    globalLimit = limit;
                }
                _cache.Set("GlobalScanLimit", globalLimit, TimeSpan.FromMinutes(10));
            }
            limit = globalLimit;

            if (!_cache.TryGetValue("ScanLimitRollingDays", out int cacheDays))
            {
                var rollingDaysSetting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays", cancellationToken);
                if (rollingDaysSetting != null && int.TryParse(rollingDaysSetting.Value, out int parsedDays))
                {
                    cacheDays = parsedDays;
                }
                else
                {
                    cacheDays = rollingDays;
                }
                _cache.Set("ScanLimitRollingDays", cacheDays, TimeSpan.FromMinutes(10));
            }
            rollingDays = cacheDays;

            var userLimit = await _dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (userLimit != null)
            {
                limit = userLimit.MaxScans;
            }

            var thresholdDate = DateTime.UtcNow.AddDays(-rollingDays);

            var userLock = _cache.GetOrCreate(userId, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return new SemaphoreSlim(1, 1);
            }) ?? new SemaphoreSlim(1, 1);

            await userLock.WaitAsync(cancellationToken);
            try
            {
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    _dbContext.ChangeTracker.Clear();

                    var usageCount = await _dbContext.UserScanUsages
                        .Where(u => u.UserId == userId && u.ScannedAt >= thresholdDate)
                        .CountAsync(cancellationToken);

                    if (usageCount >= limit)
                    {
                        return null;
                    }

                    var entity = new UserScanUsageEntity
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ScannedAt = DateTime.UtcNow
                    };
                    _dbContext.UserScanUsages.Add(entity);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return entity;
                });
            }
            finally
            {
                userLock.Release();
            }
        }

        public async Task RefundUsageAsync(UserScanUsageEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) return;

            var userLock = _cache.GetOrCreate(entity.UserId, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return new SemaphoreSlim(1, 1);
            }) ?? new SemaphoreSlim(1, 1);

            await userLock.WaitAsync(cancellationToken);
            try
            {
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    _dbContext.ChangeTracker.Clear();
                    _dbContext.UserScanUsages.Remove(entity);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                });
            }
            finally
            {
                userLock.Release();
            }
        }
    }
}
