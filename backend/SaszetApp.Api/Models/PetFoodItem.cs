using System;
using System.Collections.Generic;

namespace SaszetApp.Api.Models
{
    public class PetFoodItem
    {
        public Guid Id { get; set; }
        public string? EanCode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int Rating { get; set; }
        public List<string> Pros { get; set; } = new();
        public List<string> Cons { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public string ExtractedIngredients { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
