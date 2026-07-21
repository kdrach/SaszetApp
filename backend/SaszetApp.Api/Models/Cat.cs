using System;
using System.Collections.Generic;

namespace SaszetApp.Api.Models
{
    public class Cat
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Breed { get; set; }
        public int? Age { get; set; }
        public decimal? Weight { get; set; }
        public List<string> Allergies { get; set; } = new List<string>();
    }
}
