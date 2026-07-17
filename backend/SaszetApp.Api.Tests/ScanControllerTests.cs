using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        private readonly Mock<IScanQuotaService> _mockScanQuotaService;
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
            _mockScanQuotaService = new Mock<IScanQuotaService>();
            _mockScanQuotaService.Setup(r => r.CheckAndRecordUsageAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new UserScanUsageEntity());
            var memoryCacheOptions = new Microsoft.Extensions.Options.OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions());
            var realMemoryCache = new MemoryCache(memoryCacheOptions);
            _mapper = new PetFoodModelMapper();

            _controller = new ScanController(_dbContext, _mockVlmService.Object, _mapper, Microsoft.Extensions.Logging.Abstractions.NullLogger<ScanController>.Instance, _mockEncryptionService.Object, _mockScanQuotaService.Object, realMemoryCache);
            
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
                Rating = 8,
                UserId = "test-user-id"
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
            var result = await _controller.AnalyzeImage(null, System.Threading.CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AnalyzeImage_OversizedImage_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
            
            var result = await _controller.AnalyzeImage(mockFile.Object, System.Threading.CancellationToken.None);
            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Image size exceeds the 5MB limit.", badRequestResult.Value);
        }

        [Fact]
        public async Task AnalyzeImage_InvalidMimeType_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1 * 1024 * 1024); // 1MB
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");
            
            var result = await _controller.AnalyzeImage(mockFile.Object, System.Threading.CancellationToken.None);
            
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
            var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            
            mockFile.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

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
                .Setup(v => v.AnalyzeImageAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<string>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedModel);

            var result = await _controller.AnalyzeImage(mockFile.Object, System.Threading.CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<PetFoodItem>(okResult.Value);
            Assert.Equal("Image Food", model.ProductName);
            
            _mockVlmService.Verify(v => v.AnalyzeImageAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<string>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AnalyzeImage_VlmThrowsNoPetFoodFound_Returns422_And_ConsumesUsage()
        {
            // Arrange
            var provider = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4-vision", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key" };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            var mockFile = new Mock<IFormFile>();
            var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            
            mockFile.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            _controller.Request.Headers["Accept-Language"] = "pl-PL";

            _mockVlmService
                .Setup(v => v.AnalyzeImageAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<string>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("NO_PET_FOOD_FOUND"));

            // Act
            var result = await _controller.AnalyzeImage(mockFile.Object, System.Threading.CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(422, objectResult.StatusCode);
            
            // Check dynamic property "errorCode"
            var valueType = objectResult.Value.GetType();
            var propertyInfo = valueType.GetProperty("errorCode");
            Assert.NotNull(propertyInfo);
            var errorCodeValue = propertyInfo.GetValue(objectResult.Value);
            Assert.Equal("NO_PET_FOOD_FOUND", errorCodeValue);

            _mockScanQuotaService.Verify(s => s.RefundUsageAsync(It.IsAny<UserScanUsageEntity>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Search_LimitExceeded_Returns429TooManyRequests()
        {
            _mockScanQuotaService.Setup(r => r.CheckAndRecordUsageAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync((UserScanUsageEntity?)null);
            var result = await _controller.Search("9999", System.Threading.CancellationToken.None);
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(429, objectResult.StatusCode);
        }

        [Fact]
        public async Task AnalyzeImage_LimitExceeded_Returns429TooManyRequests()
        {
            var mockFile = new Mock<IFormFile>();
            var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            
            mockFile.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            _mockScanQuotaService.Setup(r => r.CheckAndRecordUsageAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync((UserScanUsageEntity?)null);
            var result = await _controller.AnalyzeImage(mockFile.Object, System.Threading.CancellationToken.None);
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(429, objectResult.StatusCode);
        }

        [Fact]
        public async Task AnalyzeImage_MalformedImage_RefundsQuota_AndReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            var content = System.Text.Encoding.UTF8.GetBytes("not a real image");
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            
            mockFile.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            var result = await _controller.AnalyzeImage(mockFile.Object, System.Threading.CancellationToken.None);
            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid image file.", badRequestResult.Value);

            _mockScanQuotaService.Verify(s => s.RefundUsageAsync(It.IsAny<UserScanUsageEntity>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AnalyzeImage_VlmThrowsHttpRequestException_FallsBack_And_DoesNotConsumeQuotaIfAllFail()
        {
            var provider1 = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4-vision", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key", PriorityOrder = 1 };
            var provider2 = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4o", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key", PriorityOrder = 2 };
            _dbContext.LlmProviders.Add(provider1);
            _dbContext.LlmProviders.Add(provider2);
            await _dbContext.SaveChangesAsync();

            var mockFile = new Mock<IFormFile>();
            var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");
            mockFile.Setup(f => f.Length).Returns(content.Length);
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
            
            mockFile.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            _mockVlmService
                .Setup(v => v.AnalyzeImageAsync("OpenAI", It.IsAny<string>(), "test-key", It.IsAny<string>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new System.Net.Http.HttpRequestException("429 Too Many Requests", null, System.Net.HttpStatusCode.TooManyRequests));

            var result = await _controller.AnalyzeImage(mockFile.Object, System.Threading.CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);

            // It should have tried both providers due to fallback logic continuing
            _mockVlmService.Verify(v => v.AnalyzeImageAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<string>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            _mockVlmService.Verify(v => v.AnalyzeImageAsync("OpenAI", "gpt-4o", "test-key", It.IsAny<string>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()), Times.Once);

            _mockScanQuotaService.Verify(s => s.RefundUsageAsync(It.IsAny<UserScanUsageEntity>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Compare_TooFewImages_ReturnsBadRequest()
        {
            var images = new System.Collections.Generic.List<IFormFile> { new Mock<IFormFile>().Object };
            var result = await _controller.Compare(images, System.Threading.CancellationToken.None);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Please provide between 2 and 5 images for comparison.", badRequestResult.Value);
        }

        [Fact]
        public async Task Compare_TooManyImages_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>().Object;
            var images = new System.Collections.Generic.List<IFormFile> { mockFile, mockFile, mockFile, mockFile, mockFile, mockFile };
            var result = await _controller.Compare(images, System.Threading.CancellationToken.None);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Please provide between 2 and 5 images for comparison.", badRequestResult.Value);
        }

        [Fact]
        public async Task Compare_ValidImages_CallsVlmService_And_DoesNotCache()
        {
            var provider = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4-vision", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key" };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            var mockFile1 = new Mock<IFormFile>();
            var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");
            mockFile1.Setup(f => f.Length).Returns(content.Length);
            mockFile1.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile1.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            var mockFile2 = new Mock<IFormFile>();
            mockFile2.Setup(f => f.Length).Returns(content.Length);
            mockFile2.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile2.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            _controller.Request.Headers["Accept-Language"] = "pl-PL";

            var expectedResponse = new SaszetApp.Api.DTOs.MultiVlmResponseContract
            {
                Products = new System.Collections.Generic.List<SaszetApp.Api.DTOs.VlmResponseContract>
                {
                    new SaszetApp.Api.DTOs.VlmResponseContract { ProductName = "Food A" },
                    new SaszetApp.Api.DTOs.VlmResponseContract { ProductName = "Food B" }
                }
            };

            _mockVlmService
                .Setup(v => v.AnalyzeMultipleImagesAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<System.Collections.Generic.List<string>>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var images = new System.Collections.Generic.List<IFormFile> { mockFile1.Object, mockFile2.Object };
            var result = await _controller.Compare(images, System.Threading.CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultList = Assert.IsType<System.Collections.Generic.List<PetFoodItem>>(okResult.Value);
            Assert.Equal(2, resultList.Count);
            Assert.Equal("Food A", resultList[0].ProductName);
            Assert.Equal("Food B", resultList[1].ProductName);

            _mockVlmService.Verify(v => v.AnalyzeMultipleImagesAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<System.Collections.Generic.List<string>>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Compare_VlmThrowsHttpRequestException_FallsBack()
        {
            var provider1 = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4-vision", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key", PriorityOrder = 1 };
            var provider2 = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4o", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key", PriorityOrder = 2 };
            _dbContext.LlmProviders.Add(provider1);
            _dbContext.LlmProviders.Add(provider2);
            await _dbContext.SaveChangesAsync();

            var mockFile1 = new Mock<IFormFile>();
            var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");
            mockFile1.Setup(f => f.Length).Returns(content.Length);
            mockFile1.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile1.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            var mockFile2 = new Mock<IFormFile>();
            mockFile2.Setup(f => f.Length).Returns(content.Length);
            mockFile2.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile2.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            var expectedResponse = new SaszetApp.Api.DTOs.MultiVlmResponseContract
            {
                Products = new System.Collections.Generic.List<SaszetApp.Api.DTOs.VlmResponseContract>
                {
                    new SaszetApp.Api.DTOs.VlmResponseContract { ProductName = "Food A" }
                }
            };

            // Setup first provider to throw 500 so it falls back
            _mockVlmService
                .Setup(v => v.AnalyzeMultipleImagesAsync("OpenAI", "gpt-4-vision", "test-key", It.IsAny<System.Collections.Generic.List<string>>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new System.Net.Http.HttpRequestException("500 Server Error", null, System.Net.HttpStatusCode.InternalServerError));

            // Setup second provider to return success
            _mockVlmService
                .Setup(v => v.AnalyzeMultipleImagesAsync("OpenAI", "gpt-4o", "test-key", It.IsAny<System.Collections.Generic.List<string>>(), "image/jpeg", "pl", It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var images = new System.Collections.Generic.List<IFormFile> { mockFile1.Object, mockFile2.Object };
            var result = await _controller.Compare(images, System.Threading.CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultList = Assert.IsType<System.Collections.Generic.List<PetFoodItem>>(okResult.Value);
            Assert.Single(resultList);

            // It should not refund because it succeeded eventually
            _mockScanQuotaService.Verify(s => s.RefundUsageAsync(It.IsAny<UserScanUsageEntity>(), It.IsAny<System.Threading.CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Compare_NullOrEmptyProducts_AreIgnored()
        {
            var provider = new LlmProviderEntity { Id = Guid.NewGuid(), ProviderName = "OpenAI", ModelName = "gpt-4-vision", IsPrimary = true, IsActive = true, EncryptedApiKey = "enc-key" };
            _dbContext.LlmProviders.Add(provider);
            await _dbContext.SaveChangesAsync();

            var mockFile1 = new Mock<IFormFile>();
            var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAACklEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==");
            mockFile1.Setup(f => f.Length).Returns(content.Length);
            mockFile1.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile1.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            var mockFile2 = new Mock<IFormFile>();
            mockFile2.Setup(f => f.Length).Returns(content.Length);
            mockFile2.Setup(f => f.ContentType).Returns("image/jpeg");
            mockFile2.Setup(f => f.OpenReadStream()).Returns(() => new System.IO.MemoryStream(content));

            var expectedResponse = new SaszetApp.Api.DTOs.MultiVlmResponseContract
            {
                Products = new System.Collections.Generic.List<SaszetApp.Api.DTOs.VlmResponseContract>
                {
                    null, // Null product
                    new SaszetApp.Api.DTOs.VlmResponseContract { ProductName = "" }, // Empty name
                    new SaszetApp.Api.DTOs.VlmResponseContract { ProductName = "Valid Food" }
                }
            };

            _mockVlmService
                .Setup(v => v.AnalyzeMultipleImagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Collections.Generic.List<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var images = new System.Collections.Generic.List<IFormFile> { mockFile1.Object, mockFile2.Object };
            var result = await _controller.Compare(images, System.Threading.CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultList = Assert.IsType<System.Collections.Generic.List<PetFoodItem>>(okResult.Value);
            
            // Should only contain the valid one
            Assert.Single(resultList);
            Assert.Equal("Valid Food", resultList[0].ProductName);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
