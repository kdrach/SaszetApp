using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Controllers;
using SaszetApp.Api.Data;
using Xunit;

namespace SaszetApp.Api.Tests
{
    public class AdminSettingsControllerTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly AdminSettingsController _controller;

        public AdminSettingsControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _controller = new AdminSettingsController(_dbContext);
        }

        [Fact]
        public async Task GetGlobalSettings_WhenEmpty_ReturnsDefault()
        {
            var result = await _controller.GetGlobalSettings();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<AdminSettingsController.GlobalSettingsDto>(okResult.Value);
            
            Assert.Equal(5, dto.GlobalScanLimit);
            Assert.Equal(7, dto.ScanLimitRollingDays);
        }

        [Fact]
        public async Task UpdateGlobalSettings_SavesToDb()
        {
            var dto = new AdminSettingsController.GlobalSettingsDto { GlobalScanLimit = 10, ScanLimitRollingDays = 14 };
            var result = await _controller.UpdateGlobalSettings(dto);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            var limitEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalScanLimit");
            var daysEntity = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ScanLimitRollingDays");
            
            Assert.NotNull(limitEntity);
            Assert.Equal("10", limitEntity.Value);
            
            Assert.NotNull(daysEntity);
            Assert.Equal("14", daysEntity.Value);
        }

        [Fact]
        public async Task GetUserLimit_WhenNotExists_ReturnsNotFound()
        {
            var result = await _controller.GetUserLimit("test-user");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateUserLimit_CreatesNewIfNotExist()
        {
            var dto = new AdminSettingsController.UserLimitDto { UserId = "test-user", MaxScans = 20 };
            var result = await _controller.UpdateUserLimit("test-user", dto);
            
            Assert.IsType<OkObjectResult>(result);
            var entity = await _dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == "test-user");
            
            Assert.NotNull(entity);
            Assert.Equal(20, entity.MaxScans);
        }

        [Fact]
        public async Task GetAllUsersLimits_ReturnsExpectedData()
        {
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "GlobalScanLimit", Value = "5" });
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "ScanLimitRollingDays", Value = "7" });
            
            _dbContext.UserScanLimits.Add(new UserScanLimitEntity { UserId = "user1", MaxScans = 10 });
            _dbContext.UserScanUsages.Add(new UserScanUsageEntity { Id = Guid.NewGuid(), UserId = "user1", ScannedAt = DateTime.UtcNow });
            _dbContext.UserScanUsages.Add(new UserScanUsageEntity { Id = Guid.NewGuid(), UserId = "user2", ScannedAt = DateTime.UtcNow });
            
            await _dbContext.SaveChangesAsync();

            var result = await _controller.GetAllUsersLimits();
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<AdminSettingsController.UserLimitDto>>(okResult.Value);
            var list = System.Linq.Enumerable.ToList(items);
            
            Assert.Equal(2, list.Count);
            
            var u1 = System.Linq.Enumerable.Single(list, x => x.UserId == "user1");
            Assert.Equal(10, u1.MaxScans);
            Assert.Equal(1, u1.Usage);
            
            var u2 = System.Linq.Enumerable.Single(list, x => x.UserId == "user2");
            Assert.Equal(5, u2.MaxScans);
            Assert.Equal(1, u2.Usage);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
