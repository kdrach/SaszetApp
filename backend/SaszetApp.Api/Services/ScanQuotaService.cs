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
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IMemoryCache _cache;

        public ScanQuotaService(IDbContextFactory<AppDbContext> dbContextFactory, IMemoryCache cache)
        {
            _dbContextFactory = dbContextFactory;
            _cache = cache;
        }

        public async Task<UserScanUsageEntity?> CheckAndRecordUsageAsync(string userId, CancellationToken cancellationToken = default)
        {
            int limit = 5;
            int rollingDays = 7;

            using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            if (!_cache.TryGetValue("GlobalScanLimit", out int globalLimit))
            {
                var globalLimitSetting = await dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit", cancellationToken);
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
                var rollingDaysSetting = await dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays", cancellationToken);
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

            var userLimit = await dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (userLimit != null)
            {
                limit = userLimit.MaxScans;
            }

            var thresholdDate = DateTime.UtcNow.AddDays(-rollingDays);

            var userLockLazy = _cache.GetOrCreate($"ScanLock_{userId}", entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                entry.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    if (value is Lazy<SemaphoreSlim> lazy && lazy.IsValueCreated)
                    {
                        lazy.Value.Dispose();
                    }
                });
                return new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1));
            }) ?? new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1));

            var userLock = userLockLazy.Value;

            await userLock.WaitAsync(cancellationToken);
            try
            {
                var strategy = dbContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    dbContext.ChangeTracker.Clear();

                    var usageCount = await dbContext.UserScanUsages
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
                    dbContext.UserScanUsages.Add(entity);
                    await dbContext.SaveChangesAsync(cancellationToken);

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

            var userLockLazy = _cache.GetOrCreate($"ScanLock_{entity.UserId}", entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                entry.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    if (value is Lazy<SemaphoreSlim> lazy && lazy.IsValueCreated)
                    {
                        lazy.Value.Dispose();
                    }
                });
                return new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1));
            }) ?? new Lazy<SemaphoreSlim>(() => new SemaphoreSlim(1, 1));

            var userLock = userLockLazy.Value;

            await userLock.WaitAsync(cancellationToken);
            try
            {
                using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var strategy = dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    dbContext.ChangeTracker.Clear();
                    dbContext.UserScanUsages.Remove(entity);
                    await dbContext.SaveChangesAsync(cancellationToken);
                });
            }
            finally
            {
                userLock.Release();
            }
        }
    }
}
