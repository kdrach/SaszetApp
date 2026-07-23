using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SaszetApp.Api.DTOs
{
    public class CatUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string? Breed { get; set; }

        [Range(0, 100)]
        public decimal? Weight { get; set; }
        public List<string>? Allergies { get; set; }
    }
}
