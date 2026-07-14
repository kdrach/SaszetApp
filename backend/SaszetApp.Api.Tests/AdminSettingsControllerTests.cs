using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Controllers;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models.Admin;
using SaszetApp.Api.Mappers;
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
            _controller = new AdminSettingsController(_dbContext, new AdminSettingsModelMapper());
        }

        [Fact]
        public async Task GetGlobalSettings_WhenEmpty_ReturnsDefault()
        {
            var result = await _controller.GetGlobalSettings();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<GlobalSettingsDto>(okResult.Value);
            
            Assert.Equal(5, dto.GlobalScanLimit);
            Assert.Equal(7, dto.ScanLimitRollingDays);
        }

        [Fact]
        public async Task UpdateGlobalSettings_SavesToDb()
        {
            var dto = new GlobalSettingsDto { GlobalScanLimit = 10, ScanLimitRollingDays = 14 };
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
            var dto = new UserLimitDto { UserId = "test-user", MaxScans = 20 };
            var result = await _controller.UpdateUserLimit("test-user", dto);
            
            Assert.IsType<OkObjectResult>(result);
            var entity = await _dbContext.UserScanLimits.FirstOrDefaultAsync(u => u.UserId == "test-user");
            
            Assert.NotNull(entity);
            Assert.Equal(20, entity.MaxScans);
        }

        [Fact]
        public async Task GetAllUsersLimits_ReturnsExpectedData_And_Paginates()
        {
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "GlobalScanLimit", Value = "5" });
            _dbContext.SystemSettings.Add(new SystemSettingEntity { Key = "ScanLimitRollingDays", Value = "7" });
            
            _dbContext.UserScanLimits.Add(new UserScanLimitEntity { UserId = "user1", MaxScans = 10 });
            _dbContext.UserScanUsages.Add(new UserScanUsageEntity { Id = Guid.NewGuid(), UserId = "user1", ScannedAt = DateTime.UtcNow });
            _dbContext.UserScanUsages.Add(new UserScanUsageEntity { Id = Guid.NewGuid(), UserId = "user2", ScannedAt = DateTime.UtcNow });
            _dbContext.UserScanUsages.Add(new UserScanUsageEntity { Id = Guid.NewGuid(), UserId = "user3", ScannedAt = DateTime.UtcNow });
            
            await _dbContext.SaveChangesAsync();

            var result = await _controller.GetAllUsersLimits(page: 1, pageSize: 2);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var pagedResult = Assert.IsType<PagedResult<UserLimitDetailsDto>>(okResult.Value);
            
            Assert.Equal(3, pagedResult.TotalCount);
            Assert.Equal(1, pagedResult.Page);
            Assert.Equal(2, pagedResult.PageSize);
            
            var list = pagedResult.Items.ToList();
            Assert.Equal(2, list.Count);
            
            var u1 = list.Single(x => x.UserId == "user1");
            Assert.Equal(10, u1.MaxScans);
            Assert.Equal(1, u1.Usage);
            
            var u2 = list.Single(x => x.UserId == "user2");
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
