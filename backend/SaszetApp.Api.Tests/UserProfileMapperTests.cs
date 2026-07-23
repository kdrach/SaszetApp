using System;
using System.Collections.Generic;
using Xunit;
using SaszetApp.Api.Mappers;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Tests
{
    public class UserProfileMapperTests
    {
        private readonly UserProfileMapper _mapper;

        public UserProfileMapperTests()
        {
            _mapper = new UserProfileMapper();
        }

        [Fact]
        public void MapToCat_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var entity = new CatEntity
            {
                Id = Guid.NewGuid(),
                UserId = "keycloak-uuid",
                Name = "Puszek",
                Breed = "Maine Coon",
                Age = 5,
                Weight = 6.5m,
                Allergies = new List<string> { "chicken", "beef" },
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var model = _mapper.MapToCat(entity);

            // Assert
            Assert.Equal(entity.Id, model.Id);
            Assert.Equal(entity.Name, model.Name);
            Assert.Equal(entity.Breed, model.Breed);
            Assert.Equal(entity.Age, model.Age);
            Assert.Equal(entity.Weight, model.Weight);
            Assert.Equal(entity.Allergies, model.Allergies);
        }

        [Fact]
        public void MapToUser_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var catEntity = new CatEntity
            {
                Id = Guid.NewGuid(),
                UserId = "keycloak-uuid",
                Name = "Puszek",
                Breed = "Maine Coon",
                Age = 5,
                Weight = 6.5m,
                Allergies = new List<string> { "chicken" },
                CreatedAt = DateTime.UtcNow
            };

            var userEntity = new UserEntity
            {
                Id = "keycloak-uuid",
                CreatedAt = DateTime.UtcNow,
                Cats = new List<CatEntity> { catEntity }
            };

            // Act
            var model = _mapper.MapToUser(userEntity, 10, 20); // 10 remaining scans, 20 limit

            // Assert
            Assert.Equal(userEntity.Id, model.Id);
            Assert.Equal(10, model.RemainingScans);
            Assert.Equal(20, model.MaxScans);
            Assert.Single(model.Cats);
            Assert.Equal(catEntity.Name, model.Cats[0].Name);
        }
    }
}
