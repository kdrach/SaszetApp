using SaszetApp.Api.Data;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services.Mappers
{
    public class PetFoodModelMapper : IPetFoodModelMapper
    {
        public PetFoodItem MapToModel(PetFoodItemEntity entity)
        {
            return new PetFoodItem
            {
                Id = entity.Id,
                EanCode = entity.EanCode,
                ProductName = entity.ProductName,
                Language = entity.Language,
                Rating = entity.Rating,
                Pros = entity.Pros,
                Cons = entity.Cons,
                Summary = entity.Summary,
                ExtractedIngredients = entity.ExtractedIngredients,
                CreatedAt = entity.CreatedAt
            };
        }

        public PetFoodItemEntity MapToEntity(PetFoodItem model)
        {
            return new PetFoodItemEntity
            {
                Id = model.Id,
                EanCode = model.EanCode,
                ProductName = model.ProductName,
                Language = model.Language,
                Rating = model.Rating,
                Pros = model.Pros,
                Cons = model.Cons,
                Summary = model.Summary,
                ExtractedIngredients = model.ExtractedIngredients,
                CreatedAt = model.CreatedAt
            };
        }
    }
}
