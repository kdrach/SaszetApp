using System.Linq;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Mappers
{
    public class UserProfileMapper
    {
        public Cat MapToCat(CatEntity entity)
        {
            if (entity == null) return null!;

            return new Cat
            {
                Id = entity.Id,
                Name = entity.Name,
                Breed = entity.Breed,
                Age = entity.Age,
                Weight = entity.Weight,
                Allergies = entity.Allergies?.ToList() ?? new System.Collections.Generic.List<string>()
            };
        }

        public User MapToUser(UserEntity entity, int remainingScans)
        {
            if (entity == null) return null!;

            return new User
            {
                Id = entity.Id,
                RemainingScans = remainingScans,
                Cats = entity.Cats?.Select(MapToCat).ToList() ?? new System.Collections.Generic.List<Cat>()
            };
        }
    }
}
