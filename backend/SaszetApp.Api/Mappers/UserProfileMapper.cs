using System.Linq;
using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Mappers
{
    public interface IUserProfileMapper
    {
        Cat MapToCat(CatEntity entity);
        User MapToUser(UserEntity entity, int remainingScans);
    }

    public class UserProfileMapper : IUserProfileMapper
    {
        public Cat MapToCat(CatEntity entity)
        {
            if (entity == null) throw new System.ArgumentNullException(nameof(entity));

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
            if (entity == null) throw new System.ArgumentNullException(nameof(entity));

            return new User
            {
                Id = entity.Id,
                RemainingScans = remainingScans,
                Cats = entity.Cats?.Select(MapToCat).ToList() ?? new System.Collections.Generic.List<Cat>()
            };
        }
    }
}
