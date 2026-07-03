using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services.Mappers
{
    public interface IPetFoodModelMapper
    {
        PetFoodItem MapToModel(PetFoodItemEntity entity);
        PetFoodItemEntity MapToEntity(PetFoodItem model);
    }
}
