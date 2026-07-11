using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    public class ScanControllerTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IVlmService> _mockVlmService;
        private readonly Mock<IEncryptionService> _mockEncryptionService;
        private readonly IPetFoodModelMapper _mapper;
        private readonly ScanController _controller;

        public ScanControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _mockVlmService = new Mock<IVlmService>();
            _mockEncryptionService = new Mock<IEncryptionService>();
            _mockEncryptionService.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("test-key");
            _mapper = new PetFoodModelMapper();

            _controller = new ScanController(_dbContext, _mockVlmService.Object, _mapper, Microsoft.Extensions.Logging.Abstractions.NullLogger<ScanController>.Instance, _mockEncryptionService.Object);
            
            var httpContext = new DefaultHttpContext();
            var identity = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id")
            }, "TestAuthType");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);
            httpContext.User = user;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Search_EmptyQuery_ReturnsBadRequest()
        {
            var result = await _controller.Search("", System.Threading.CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Search_CacheHit_ReturnsFromDb()
        {
            // Arrange
            var entity = new PetFoodItemEntity
            {
                Id = Guid.NewGuid(),
                EanCode = "12345678",
                ProductName = "Test Food",
                Language = "pl",
                Rating = 8
            };
            _dbContext.PetFoodItems.Add(entity);
            await _dbContext.SaveChangesAsync();

            _controller.Request.Headers["Accept-Language"] = "pl-PL";

            // Act
            var result = await _controller.Search("12345678", System.Threading.CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<PetFoodItem>(okResult.Value);
            Assert.Equal("12345678", model.EanCode);
            Assert.Equal("Test Food", model.ProductName);
            
            // Ensure VLM was not called
            _mockVlmService.Verify(v => v.AnalyzeProductAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Search_CacheMiss_CallsVlmService()
        {
            // Arrange
            var provider = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key" };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            _controller.Request.Headers["Accept-Language"] = "en-US";
            var expectedModel = new SaszetApp.Api.DTOs.VlmResponseContract
            {
                ProductName = "New Food",
                Rating = 5,
                Pros = new System.Collections.Generic.List<string>(),
                Cons = new System.Collections.Generic.List<string>(),
                Summary = "Summary",
                ExtractedIngredients = "Ingredients"
            };

            _mockVlmService
                .Setup(v => v.AnalyzeProductAsync("OpenAI", "gpt-4", "test-key", "9999", "en", It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedModel);

            // Act
            var result = await _controller.Search("9999", System.Threading.CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<PetFoodItem>(okResult.Value);
            Assert.Equal("New Food", model.ProductName);
            Assert.Equal("en", model.Language);

            _mockVlmService.Verify(v => v.AnalyzeProductAsync("OpenAI", "gpt-4", "test-key", "9999", "en", It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
        [Fact]
        public async Task AnalyzeImage_NoImage_ReturnsBadRequest()
        {
            var result = await _controller.AnalyzeImage(null, ScanMode.Ingredients, System.Threading.CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AnalyzeImage_OversizedImage_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
            
            var result = await _controller.AnalyzeImage(mockFile.Object, ScanMode.Ingredients, System.Threading.CancellationToken.None);
            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Image size exceeds the 5MB limit.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeImage_InvalidMimeType_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1 * 1024 * 1024); // 1MB
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            
            var result = await _controller.AnalyzeImage(mockFile.Object, ScanMode.Ingredients, System.Threading.CancellationToken.None);
            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unsupported image format. Use JPEG, PNG, or WebP.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeImage_ValidImage_CallsVlmService()
        {
            var provider = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4-vision", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key" };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            var mockFile = new Mock<IFormFile>();
            var content = "fake image content"u8.ToArray();
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            
            var ms = new System.IO.MemoryStream(content);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<System.IO.Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                .Callback<System.IO.Stream, System.Threading.CancellationToken>((stream, token) => ms.CopyTo(stream))
                .Returns(Task.CompletedTask);

            _controller.Request.Headers["Accept-Language"] = "pl-PL";

            var expectedModel = new SaszetApp.Api.DTOs.VlmResponseContract
            {
                ProductName = "Image Food",
                Rating = 8,
                Pros = new System.Collections.Generic.List<string>(),
                Cons = new System.Collections.Generic.List<string>(),
                Summary = "Summary",
                ExtractedIngredients = "Ingredients"
            };

            _mockVlmService
                .Setup(v => v.AnalyzeImageAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<string>(), "image/jpeg", ScanMode.Ingredients, "pl", It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedModel);

            var result = await _controller.AnalyzeImage(mockFile.Object, ScanMode.Ingredients, System.Threading.CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<PetFoodItem>(okResult.Value);
            Assert.Equal("Image Food", model.ProductName);
            
            _mockVlmService.Verify(v => v.AnalyzeImageAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<string>(), "image/jpeg", ScanMode.Ingredients, "pl", It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AnalyzeImage_VlmThrowsNoPetFoodFound_Returns422UnprocessableEntity()
        {
            // Arrange
            var provider = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4-vision", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key" };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            var mockFile = new Mock<IFormFile>();
            var content = "fake image content"u8.ToArray();
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            
            var ms = new System.IO.MemoryStream(content);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<System.IO.Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                .Callback<System.IO.Stream, System.Threading.CancellationToken>((stream, token) => ms.CopyTo(stream))
                .Returns(Task.CompletedTask);

            _controller.Request.Headers["Accept-Language"] = "pl-PL";

            _mockVlmService
                .Setup(v => v.AnalyzeImageAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<string>(), "image/jpeg", ScanMode.Ingredients, "pl", It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("NO_PET_FOOD_FOUND"));

            // Act
            var result = await _controller.AnalyzeImage(mockFile.Object, ScanMode.Ingredients, System.Threading.CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(422, objectResult.StatusCode);
            
            // Check dynamic property "errorCode"
            var valueType = objectResult.Value.GetType();
            var propertyInfo = valueType.GetProperty("errorCode");
            Assert.NotNull(propertyInfo);
            var errorCodeValue = propertyInfo.GetValue(objectResult.Value);
            Assert.Equal("NO_PET_FOOD_FOUND", errorCodeValue);
        }


        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
