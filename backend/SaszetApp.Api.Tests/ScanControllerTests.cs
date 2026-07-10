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
        private readonly IPetFoodModelMapper _mapper;
        private readonly ScanController _controller;

        public ScanControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _mockVlmService = new Mock<IVlmService>();
            _mapper = new PetFoodModelMapper();

            _controller = new ScanController(_dbContext, _mockVlmService.Object, _mapper);
            
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Search_EmptyQuery_ReturnsBadRequest()
        {
            var result = await _controller.Search("");
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
            var result = await _controller.Search("12345678");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<PetFoodItem>(okResult.Value);
            Assert.Equal("12345678", model.EanCode);
            Assert.Equal("Test Food", model.ProductName);
            
            // Ensure VLM was not called
            _mockVlmService.Verify(v => v.AnalyzeProductAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Search_CacheMiss_CallsVlmService()
        {
            // Arrange
            _controller.Request.Headers["Accept-Language"] = "en-US";
            var expectedModel = new PetFoodItem
            {
                Id = Guid.NewGuid(),
                ProductName = "New Food",
                Language = "en",
                Rating = 5
            };

            _mockVlmService
                .Setup(v => v.AnalyzeProductAsync("9999", "en"))
                .ReturnsAsync(expectedModel);

            // Act
            var result = await _controller.Search("9999");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<PetFoodItem>(okResult.Value);
            Assert.Equal("New Food", model.ProductName);
            Assert.Equal("en", model.Language);

            _mockVlmService.Verify(v => v.AnalyzeProductAsync("9999", "en"), Times.Once);
        }
        [Fact]
        public async Task AnalyzeImage_NoImage_ReturnsBadRequest()
        {
            var result = await _controller.AnalyzeImage(null, ScanMode.Ingredients);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AnalyzeImage_OversizedImage_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
            
            var result = await _controller.AnalyzeImage(mockFile.Object, ScanMode.Ingredients);
            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Image size exceeds the 5MB limit.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeImage_InvalidMimeType_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1 * 1024 * 1024); // 1MB
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            
            var result = await _controller.AnalyzeImage(mockFile.Object, ScanMode.Ingredients);
            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unsupported image format. Use JPEG, PNG, or WebP.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeImage_ValidImage_CallsVlmService()
        {
            var mockFile = new Mock<IFormFile>();
            var content = "fake image content"u8.ToArray();
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            
            var ms = new System.IO.MemoryStream(content);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<System.IO.Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                .Callback<System.IO.Stream, System.Threading.CancellationToken>((stream, token) => ms.CopyTo(stream))
                .Returns(Task.CompletedTask);

            _controller.Request.Headers["Accept-Language"] = "pl-PL";

            var expectedModel = new PetFoodItem
            {
                Id = Guid.NewGuid(),
                ProductName = "Image Food",
                Language = "pl",
                Rating = 8
            };

            _mockVlmService
                .Setup(v => v.AnalyzeImageAsync(It.IsAny<string>(), "image/jpeg", ScanMode.Ingredients, "pl"))
                .ReturnsAsync(expectedModel);

            var result = await _controller.AnalyzeImage(mockFile.Object, ScanMode.Ingredients);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<PetFoodItem>(okResult.Value);
            Assert.Equal("Image Food", model.ProductName);
            
            _mockVlmService.Verify(v => v.AnalyzeImageAsync(It.IsAny<string>(), "image/jpeg", ScanMode.Ingredients, "pl"), Times.Once);
        }

        [Fact]
        public async Task AnalyzeImage_VlmThrowsNoPetFoodFound_Returns422UnprocessableEntity()
        {
            // Arrange
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
                .Setup(v => v.AnalyzeImageAsync(It.IsAny<string>(), "image/jpeg", ScanMode.Ingredients, "pl"))
                .ThrowsAsync(new InvalidOperationException("NO_PET_FOOD_FOUND"));

            // Act
            var result = await _controller.AnalyzeImage(mockFile.Object, ScanMode.Ingredients);

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
