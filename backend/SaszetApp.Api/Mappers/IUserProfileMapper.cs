using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Mappers
{
    public interface IUserProfileMapper
    {
        Cat MapToCat(CatEntity entity);
        User MapToUser(UserEntity entity, int remainingScans);
    }
}
