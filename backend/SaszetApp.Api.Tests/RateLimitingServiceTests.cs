using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using SaszetApp.Api.Services;

namespace SaszetApp.Api.Tests
{
    public class RateLimitingServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IRateLimitingService _rateLimitingService;

        public RateLimitingServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _rateLimitingService = new RateLimitingService(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task CheckLimitAsync_NoSettings_UsesDefaults_AllowsUpTo5Scans()
        {
            var userId = "user1";
            for(int i=0; i<5; i++)
            {
                var allowed = await _rateLimitingService.CheckLimitAsync(userId);
                Assert.True(allowed);
                await _rateLimitingService.RecordUsageAsync(userId);
            }

            var allowedAfter5 = await _rateLimitingService.CheckLimitAsync(userId);
            Assert.False(allowedAfter5);
        }

        [Fact]
        public async Task CheckLimitAsync_UserLimitOverridesGlobalLimit()
        {
            var userId = "user2";
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "GlobalScanLimit", Value = "2" });
            _dbContext.UserScanLimits.Add(new UserScanLimitEntity { UserId = userId, MaxScans = 10 });
            await _dbContext.SaveChangesAsync();

            for(int i=0; i<10; i++)
            {
                var allowed = await _rateLimitingService.CheckLimitAsync(userId);
                Assert.True(allowed);
                await _rateLimitingService.RecordUsageAsync(userId);
            }

            var allowedAfter10 = await _rateLimitingService.CheckLimitAsync(userId);
            Assert.False(allowedAfter10);
        }

        [Fact]
        public async Task CheckLimitAsync_OldScansAreIgnored()
        {
            var userId = "user3";
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "GlobalScanLimit", Value = "1" });
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "ScanLimitRollingDays", Value = "7" });
            await _dbContext.SaveChangesAsync();

            // Insert old usage 8 days ago
            _dbContext.UserScanUsages.Add(new UserScanUsageEntity { Id = Guid.NewGuid(), UserId = userId, ScannedAt = DateTime.UtcNow.AddDays(-8) });
            await _dbContext.SaveChangesAsync();

            var allowed = await _rateLimitingService.CheckLimitAsync(userId);
            Assert.True(allowed);
        }
    }
}
