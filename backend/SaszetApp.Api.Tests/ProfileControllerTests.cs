using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaszetApp.Api.Controllers;
using SaszetApp.Api.Data;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Mappers;
using SaszetApp.Api.Models;
using SaszetApp.Api.Services;
using Xunit;

namespace SaszetApp.Api.Tests
{
    public class ProfileControllerTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IScanQuotaService> _mockScanQuotaService;
        private readonly IUserProfileMapper _mapper;
        private readonly ProfileController _controller;

        public ProfileControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _mockScanQuotaService = new Mock<IScanQuotaService>();
            _mapper = new UserProfileMapper();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user123")
            }, "TestAuthType"));

            _controller = new ProfileController(_dbContext, _mockScanQuotaService.Object, _mapper)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                }
            };
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsUserProfileWithCatsAndScans()
        {
            // Arrange
            _dbContext.Users.Add(new UserEntity { Id = "user123" });
            _dbContext.Cats.Add(new CatEntity { Id = Guid.NewGuid(), UserId = "user123", Name = "Filemon", Breed = "Dachowiec", Weight = 4.5m });
            await _dbContext.SaveChangesAsync();

            _mockScanQuotaService.Setup(s => s.GetRemainingScansAsync("user123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

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
            _dbContext.Users.Add(new UserEntity { Id = "user123" });
            await _dbContext.SaveChangesAsync();

            var createDto = new CatCreateDto
            {
                Name = "Bonifacy",
                Breed = "Maine Coon",
                Weight = 6.2m,
                Allergies = new List<string> { "Kurczak" }
            };

            // Act
            var result = await _controller.AddCatAsync(createDto, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var cat = Assert.IsType<Cat>(createdResult.Value);
            Assert.Equal("Bonifacy", cat.Name);
            Assert.Equal("Maine Coon", cat.Breed);
            Assert.Equal(6.2m, cat.Weight);
            Assert.Contains("Kurczak", cat.Allergies);

            var catInDb = await _dbContext.Cats.FirstOrDefaultAsync(c => c.Name == "Bonifacy");
            Assert.NotNull(catInDb);
        }

        [Fact]
        public async Task DeleteCatAsync_RemovesCat()
        {
            // Arrange
            var catId = Guid.NewGuid();
            _dbContext.Users.Add(new UserEntity { Id = "user123" });
            _dbContext.Cats.Add(new CatEntity { Id = catId, UserId = "user123", Name = "Filemon" });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteCatAsync(catId, CancellationToken.None);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var catInDb = await _dbContext.Cats.FirstOrDefaultAsync(c => c.Id == catId);
            Assert.Null(catInDb);
        }

        [Fact]
        public async Task AddCatAsync_ReturnsBadRequest_WhenCatLimitReached()
        {
            // Arrange
            _dbContext.Users.Add(new UserEntity { Id = "user123" });
            for (int i = 0; i < 20; i++)
            {
                _dbContext.Cats.Add(new CatEntity { Id = Guid.NewGuid(), UserId = "user123", Name = $"Cat{i}" });
            }
            await _dbContext.SaveChangesAsync();

            var createDto = new CatCreateDto
            {
                Name = "TooMany",
                Breed = "Maine Coon",
                Weight = 6.2m
            };

            // Act
            var result = await _controller.AddCatAsync(createDto, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Maximum number of cats reached.", badRequestResult.Value);
        }
    }
}
