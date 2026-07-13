using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SaszetApp.Api.Data;
using SaszetApp.Api.Services;

namespace SaszetApp.Api.Tests
{
    public class ScanQuotaServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IScanQuotaService _scanQuotaService;
        private readonly IMemoryCache _cache;

        public ScanQuotaServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _cache = new MemoryCache(new MemoryCacheOptions());
            _scanQuotaService = new ScanQuotaService(_dbContext, _cache);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _cache.Dispose();
        }

        [Fact]
        public async Task CheckAndRecordUsageAsync_NoSettings_UsesDefaults_AllowsUpTo5Scans()
        {
            var userId = "user1";
            for(int i=0; i<5; i++)
            {
                var usage = await _scanQuotaService.CheckAndRecordUsageAsync(userId);
                Assert.NotNull(usage);
            }

            var allowedAfter5 = await _scanQuotaService.CheckAndRecordUsageAsync(userId);
            Assert.Null(allowedAfter5);
        }

        [Fact]
        public async Task CheckAndRecordUsageAsync_UserLimitOverridesGlobalLimit()
        {
            var userId = "user2";
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "GlobalScanLimit", Value = "2" });
            _dbContext.UserScanLimits.Add(new UserScanLimitEntity { UserId = userId, MaxScans = 10 });
            await _dbContext.SaveChangesAsync();

            for(int i=0; i<10; i++)
            {
                var usage = await _scanQuotaService.CheckAndRecordUsageAsync(userId);
                Assert.NotNull(usage);
            }

            var allowedAfter10 = await _scanQuotaService.CheckAndRecordUsageAsync(userId);
            Assert.Null(allowedAfter10);
        }

        [Fact]
        public async Task CheckAndRecordUsageAsync_OldScansAreIgnored()
        {
            var userId = "user3";
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "GlobalScanLimit", Value = "1" });
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "ScanLimitRollingDays", Value = "7" });
            await _dbContext.SaveChangesAsync();

            // Insert old usage 8 days ago
            _dbContext.UserScanUsages.Add(new UserScanUsageEntity { Id = Guid.NewGuid(), UserId = userId, ScannedAt = DateTime.UtcNow.AddDays(-8) });
            await _dbContext.SaveChangesAsync();

            var usage = await _scanQuotaService.CheckAndRecordUsageAsync(userId);
            Assert.NotNull(usage);
        }

        [Fact]
        public async Task CheckAndRecordUsageAsync_ConcurrentRequests_Respected()
        {
            var userId = "user4";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            using var cache = new MemoryCache(new MemoryCacheOptions());

            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                using var context = new AppDbContext(options);
                var service = new ScanQuotaService(context, cache);

                await service.CheckAndRecordUsageAsync(userId);
            });

            await Task.WhenAll(tasks);

            using var verifyContext = new AppDbContext(options);
            var usages = await verifyContext.UserScanUsages.CountAsync(u => u.UserId == userId);
            
            Assert.Equal(5, usages);
        }
    }
}
