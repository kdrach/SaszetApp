using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SaszetApp.Api.Controllers;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Models;
using SaszetApp.Api.Services;
using Xunit;

namespace SaszetApp.Api.Tests
{
    public class ProfileControllerTests
    {
        private readonly Mock<IUserProfileService> _mockUserProfileService;
        private readonly ProfileController _controller;

        public ProfileControllerTests()
        {
            _mockUserProfileService = new Mock<IUserProfileService>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            }, "TestAuthType"));

            _controller = new ProfileController(_mockUserProfileService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsUserProfile()
        {
            // Arrange
            var userProfile = new User
            {
                Id = "user123",
                RemainingScans = 3,
                Cats = new List<Cat>
                {
                    new Cat { Id = Guid.NewGuid(), Name = "Filemon", Breed = "Dachowiec", Weight = 4.5m }
                }
            };

            _mockUserProfileService.Setup(s => s.GetProfileAsync("user123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(userProfile);

            // Act
            var result = await _controller.GetProfileAsync(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var user = Assert.IsType<User>(okResult.Value);
            Assert.Equal("user123", user.Id);
            Assert.Equal(3, user.RemainingScans);
            Assert.Single(user.Cats);
            Assert.Equal("Filemon", user.Cats[0].Name);
        }

        [Fact]
        public async Task AddCatAsync_CreatesCatAndReturnsIt()
        {
            // Arrange
            var createDto = new CatCreateDto
            {
                Name = "Bonifacy",
                Breed = "Maine Coon",
                Weight = 6.2m,
                Allergies = new List<string> { "Kurczak" }
            };

            var createdCat = new Cat
            {
                Id = Guid.NewGuid(),
                Name = "Bonifacy",
                Breed = "Maine Coon",
                Weight = 6.2m,
                Allergies = new List<string> { "Kurczak" }
            };

            _mockUserProfileService.Setup(s => s.AddCatAsync("user123", createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdCat);

            // Act
            var result = await _controller.AddCatAsync(createDto, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var cat = Assert.IsType<Cat>(createdResult.Value);
            Assert.Equal("Bonifacy", cat.Name);
            Assert.Equal("Maine Coon", cat.Breed);
            Assert.Equal(6.2m, cat.Weight);
            Assert.Contains("Kurczak", cat.Allergies);
        }

        [Fact]
        public async Task DeleteCatAsync_RemovesCatAndReturnsNoContent()
        {
            // Arrange
            var catId = Guid.NewGuid();
            _mockUserProfileService.Setup(s => s.DeleteCatAsync("user123", catId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteCatAsync(catId, CancellationToken.None);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteCatAsync_ReturnsNotFound_WhenCatDoesNotExist()
        {
            // Arrange
            var catId = Guid.NewGuid();
            _mockUserProfileService.Setup(s => s.DeleteCatAsync("user123", catId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteCatAsync(catId, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddCatAsync_ReturnsBadRequest_WhenCatLimitReached()
        {
            // Arrange
            var createDto = new CatCreateDto
            {
                Name = "TooMany",
                Breed = "Maine Coon",
                Weight = 6.2m
            };

            _mockUserProfileService.Setup(s => s.AddCatAsync("user123", createDto, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Maximum number of cats reached."));

            // Act
            var result = await _controller.AddCatAsync(createDto, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Maximum number of cats reached.", badRequestResult.Value);
        }
    }
}
