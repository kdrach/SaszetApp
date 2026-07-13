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

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
