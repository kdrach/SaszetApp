using System;
using System.Collections.Generic;

namespace SaszetApp.Api.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public int RemainingScans { get; set; }
        public int MaxScans { get; set; }
        public List<Cat> Cats { get; set; } = new List<Cat>();
    }
}
