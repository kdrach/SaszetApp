using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
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
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _dbContext;
        private readonly ILlmProviderModelMapper _mapper;
        private readonly Mock<IEncryptionService> _mockEncryption;
        private readonly Mock<IVlmService> _mockVlmService;
        private readonly AdminProviderController _controller;

        public AdminProviderControllerTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new AppDbContext(options);
            _dbContext.Database.EnsureCreated();
            
            _mapper = new LlmProviderModelMapper();
            _mockEncryption = new Mock<IEncryptionService>();
            _mockVlmService = new Mock<IVlmService>();
            
            _mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns((string s) => "enc_" + s);
            _mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns((string s) => s.Replace("enc_", ""));
            
            var mockMemoryCache = new Mock<IMemoryCache>();

            _controller = new AdminProviderController(_dbContext, _mapper, _mockEncryption.Object, _mockVlmService.Object, mockMemoryCache.Object);
        }

        [Fact]
        public async Task GetProviders_MasksApiKeys()
        {
            // Arrange
            _dbContext.LlmProviders.Add(new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "OpenAI",
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
            Assert.Equal("OpenAI", model.ProviderName);
        }

        [Fact]
        public async Task CreateProvider_SetsExistingPrimaryToFalse()
        {
            // Arrange
            var existing = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "OpenAI",
                IsPrimary = true
            };
            _dbContext.LlmProviders.Add(existing);
            await _dbContext.SaveChangesAsync();

            var dto = new AdminProviderController.CreateProviderDto
            {
                ProviderName = "Anthropic",
                ApiKey = "new_secret",
                IsPrimary = true
            };

            // Act
            var result = await _controller.CreateProvider(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            _dbContext.ChangeTracker.Clear();
            var dbOld = await _dbContext.LlmProviders.FindAsync(existing.Id);
            Assert.False(dbOld.IsPrimary);
            
            var dbNew = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.ProviderName == "Anthropic");
            Assert.NotNull(dbNew);
            Assert.True(dbNew.IsPrimary);
            Assert.Equal("enc_new_secret", dbNew.EncryptedApiKey);
        }

        [Fact]
        public async Task CreateProvider_UpdatesExistingProvider()
        {
            // Arrange
            var existing = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "OpenAI",
                ModelName = "old_model",
                EncryptedApiKey = "enc_old_key",
                IsPrimary = false,
                IsActive = true
            };
            _dbContext.LlmProviders.Add(existing);
            await _dbContext.SaveChangesAsync();

            var dto = new AdminProviderController.CreateProviderDto
            {
                ProviderName = "OpenAI",
                ModelName = "new_model",
                ApiKey = "new_key",
                IsPrimary = true,
                IsActive = false
            };

            // Act
            var result = await _controller.CreateProvider(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _dbContext.ChangeTracker.Clear();
            var newEntity = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.ModelName == "new_model");
            Assert.NotNull(newEntity);
            Assert.NotEqual(existing.Id, newEntity.Id);
            Assert.Equal("new_model", newEntity.ModelName);
            Assert.Equal("enc_new_key", newEntity.EncryptedApiKey);
            Assert.True(newEntity.IsPrimary);
            Assert.False(newEntity.IsActive);
        }

        [Fact]
        public async Task UpdateProvider_UpdatesExistingProvider()
        {
            // Arrange
            var existing = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "OpenAI",
                ModelName = "old_model",
                EncryptedApiKey = "enc_old_key",
                IsPrimary = false,
                IsActive = true
            };
            _dbContext.LlmProviders.Add(existing);
            await _dbContext.SaveChangesAsync();

            var dto = new AdminProviderController.CreateProviderDto
            {
                ProviderName = "OpenAI",
                ModelName = "new_model",
                ApiKey = "new_key",
                IsPrimary = true,
                IsActive = false
            };

            // Act
            var result = await _controller.UpdateProvider(existing.Id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _dbContext.ChangeTracker.Clear();
            var updated = await _dbContext.LlmProviders.FindAsync(existing.Id);
            Assert.NotNull(updated);
            Assert.Equal("new_model", updated.ModelName);
            Assert.Equal("enc_new_key", updated.EncryptedApiKey);
            Assert.True(updated.IsPrimary);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task CreateProvider_KeepsExistingApiKey()
        {
            // Arrange
            var existing = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "Anthropic",
                ModelName = "old_model",
                EncryptedApiKey = "enc_old_key",
                IsPrimary = false,
                IsActive = true
            };
            _dbContext.LlmProviders.Add(existing);
            await _dbContext.SaveChangesAsync();

            var dto = new AdminProviderController.CreateProviderDto
            {
                ProviderName = "Anthropic",
                ModelName = "new_model",
                ApiKey = "KEEP_EXISTING",
                IsPrimary = true,
                IsActive = false
            };

            // Act
            var result = await _controller.CreateProvider(dto);

            // Assert
            _dbContext.ChangeTracker.Clear();
            var updated = await _dbContext.LlmProviders.FindAsync(existing.Id);
            Assert.Equal("enc_old_key", updated.EncryptedApiKey);
        }

        [Fact]
        public async Task CreateProvider_FailsForNewProviderWithKeepExisting()
        {
            // Arrange
            var dto = new AdminProviderController.CreateProviderDto
            {
                ProviderName = "Gemini",
                ModelName = "new_model",
                ApiKey = "KEEP_EXISTING",
                IsPrimary = true,
                IsActive = false
            };

            // Act
            var result = await _controller.CreateProvider(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("KEEP_EXISTING", badRequest.Value.ToString());
        }

        [Fact]
        public async Task TestConnection_ReturnsBadRequest_OnHttpFailure()
        {
            // Arrange
            var provider = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "OpenAI",
                EncryptedApiKey = "enc_secret"
            };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            _mockVlmService
               .Setup(v => v.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("Connection test failed."));

            // Act
            var result = await _controller.TestConnection(provider.Id, default);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Connection test failed.", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task TestConnection_Gemini_CallsServiceCorrectly()
        {
            // Arrange
            var provider = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "Gemini",
                ModelName = "test-model",
                EncryptedApiKey = "enc_secret"
            };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            _mockVlmService
               .Setup(v => v.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
               .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.TestConnection(provider.Id, default);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockVlmService.Verify(v => v.TestConnectionAsync(provider.ProviderName, provider.ModelName, It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TestConnection_Anthropic_CallsServiceCorrectly()
        {
            // Arrange
            var provider = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "Anthropic",
                ModelName = "test-model",
                EncryptedApiKey = "enc_secret"
            };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            _mockVlmService
               .Setup(v => v.TestConnectionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
               .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.TestConnection(provider.Id, default);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockVlmService.Verify(v => v.TestConnectionAsync(provider.ProviderName, provider.ModelName, It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetPrimary_ChangesPrimaryProvider()
        {
            // Arrange
            var existingPrimary = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "OpenAI",
                IsPrimary = true
            };
            var newPrimary = new LlmProviderEntity
            {
                Id = Guid.NewGuid(),
                ProviderName = "Anthropic",
                IsPrimary = false
            };
            _dbContext.LlmProviders.AddRange(existingPrimary, newPrimary);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.SetPrimary(newPrimary.Id);

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            
            _dbContext.ChangeTracker.Clear();
            var dbOldPrimary = await _dbContext.LlmProviders.FindAsync(existingPrimary.Id);
            Assert.False(dbOldPrimary.IsPrimary);
            
            var dbNewPrimary = await _dbContext.LlmProviders.FindAsync(newPrimary.Id);
            Assert.True(dbNewPrimary.IsPrimary);
        }


        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            _connection.Dispose();
        }
    }
}
