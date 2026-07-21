using System.Collections.Generic;

namespace SaszetApp.Api.DTOs
{
    public class CatCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public decimal? Weight { get; set; }
        public List<string>? Allergies { get; set; }
    }
}
