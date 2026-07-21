using System;
using System.Collections.Generic;

namespace SaszetApp.Api.Data
{
    public class CatEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public int? Age { get; set; }
        public decimal? Weight { get; set; }
        public List<string> Allergies { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public UserEntity? User { get; set; }
    }
}
