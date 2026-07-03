using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaszetApp.Api.Controllers;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models;
using SaszetApp.Api.Services;
using SaszetApp.Api.Services.Mappers;
using Xunit;

namespace SaszetApp.Api.Tests
{
    public class AdminProviderControllerTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly ILlmProviderModelMapper _mapper;
        private readonly Mock<IEncryptionService> _mockEncryption;
        private readonly AdminProviderController _controller;

        public AdminProviderControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _mapper = new LlmProviderModelMapper();
            _mockEncryption = new Mock<IEncryptionService>();
            
            _mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns((string s) => "enc_" + s);
            _mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns((string s) => s.Replace("enc_", ""));

            _controller = new AdminProviderController(_dbContext, _mapper, _mockEncryption.Object);
        }

        [Fact]
        public async Task GetProviders_MasksApiKeys()
        {
            // Arrange
            _dbContext.LlmProviders.Add(new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "Test",
                EncryptedApiKey = "secret_encrypted",
                IsPrimary = true
            });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetProviders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var models = Assert.IsAssignableFrom<IEnumerable<LlmProvider>>(okResult.Value);
            var model = models.First();
            
            Assert.Equal("********", model.EncryptedApiKey);
            Assert.Equal("Test", model.ProviderName);
        }

        [Fact]
        public async Task CreateProvider_SetsExistingPrimaryToFalse()
        {
            // Arrange
            var existing = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "Old Primary",
                IsPrimary = true
            };
            _dbContext.LlmProviders.Add(existing);
            await _dbContext.SaveChangesAsync();

            var dto = new AdminProviderController.CreateProviderDto
            {
                ProviderName = "New Primary",
                ApiKey = "new_secret",
                IsPrimary = true
            };

            // Act
            var result = await _controller.CreateProvider(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            var dbOld = await _dbContext.LlmProviders.FindAsync(existing.Id);
            Assert.False(dbOld.IsPrimary);
            
            var dbNew = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.ProviderName == "New Primary");
            Assert.NotNull(dbNew);
            Assert.True(dbNew.IsPrimary);
            Assert.Equal("enc_new_secret", dbNew.EncryptedApiKey);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
