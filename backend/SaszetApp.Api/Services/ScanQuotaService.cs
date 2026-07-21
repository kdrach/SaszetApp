using System;
using System.Collections.Generic;
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
        private class RefCountedSemaphore
        {
            public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
            public int RefCount { get; set; }
        }

        private static readonly Dictionary<string, RefCountedSemaphore> _userLocks = new();
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IMemoryCache _cache;

        public ScanQuotaService(IDbContextFactory<AppDbContext> dbContextFactory, IMemoryCache cache)
        {
            _dbContextFactory = dbContextFactory;
            _cache = cache;
        }

        public async Task<UserScanUsageEntity?> CheckAndRecordUsageAsync(string userId, CancellationToken cancellationToken = default)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var (limit, rollingDays) = await GetSettingsAsync(dbContext, cancellationToken);

            var userLimit = await dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (userLimit != null)
            {
                limit = userLimit.MaxScans;
            }

            var thresholdDate = DateTime.UtcNow.AddDays(-rollingDays);

            RefCountedSemaphore userLock;
            lock (_userLocks)
            {
                if (!_userLocks.TryGetValue(userId, out userLock))
                {
                    userLock = new RefCountedSemaphore { RefCount = 1 };
                    _userLocks[userId] = userLock;
                }
                else
                {
                    userLock.RefCount++;
                }
            }

            bool lockAcquired = false;
            try
            {
                await userLock.Semaphore.WaitAsync(cancellationToken);
                lockAcquired = true;

                var entityId = Guid.NewGuid();
                var strategy = dbContext.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    var existingEntity = await dbContext.UserScanUsages.FirstOrDefaultAsync(u => u.Id == entityId, cancellationToken);
                    if (existingEntity != null)
                    {
                        return existingEntity;
                    }

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
                        Id = entityId,
                        UserId = userId,
                        ScannedAt = DateTime.UtcNow
                    };
                    dbContext.UserScanUsages.Add(entity);
                    await dbContext.SaveChangesAsync(CancellationToken.None);

                    return entity;
                });
            }
            finally
            {
                if (lockAcquired)
                {
                    userLock.Semaphore.Release();
                }
                lock (_userLocks)
                {
                    userLock.RefCount--;
                    if (userLock.RefCount == 0)
                    {
                        _userLocks.Remove(userId);
                        userLock.Semaphore.Dispose();
                    }
                }
            }
        }

        public async Task RefundUsageAsync(UserScanUsageEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) return;

            RefCountedSemaphore userLock;
            lock (_userLocks)
            {
                if (!_userLocks.TryGetValue(entity.UserId, out userLock))
                {
                    userLock = new RefCountedSemaphore { RefCount = 1 };
                    _userLocks[entity.UserId] = userLock;
                }
                else
                {
                    userLock.RefCount++;
                }
            }

            bool lockAcquired = false;
            try
            {
                await userLock.Semaphore.WaitAsync(CancellationToken.None);
                lockAcquired = true;

                using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
                var strategy = dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await dbContext.UserScanUsages.Where(u => u.Id == entity.Id).ExecuteDeleteAsync(CancellationToken.None);
                });
            }
            finally
            {
                if (lockAcquired)
                {
                    userLock.Semaphore.Release();
                }
                lock (_userLocks)
                {
                    userLock.RefCount--;
                    if (userLock.RefCount == 0)
                    {
                        _userLocks.Remove(entity.UserId);
                        userLock.Semaphore.Dispose();
                    }
                }
            }
        }
        
        private async Task<(int Limit, int RollingDays)> GetSettingsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
        {
            int limit = 5;
            int rollingDays = 7;

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

            return (limit, rollingDays);
        }

        public async Task<int> GetRemainingScansAsync(string userId, CancellationToken cancellationToken = default)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var (limit, rollingDays) = await GetSettingsAsync(dbContext, cancellationToken);

            var userLimit = await dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (userLimit != null)
            {
                limit = userLimit.MaxScans;
            }

            var thresholdDate = DateTime.UtcNow.AddDays(-rollingDays);
            
            var usageCount = await dbContext.UserScanUsages
                .Where(u => u.UserId == userId && u.ScannedAt >= thresholdDate)
                .CountAsync(cancellationToken);

            var remaining = limit - usageCount;
            return remaining < 0 ? 0 : remaining;
        }
    }
}
