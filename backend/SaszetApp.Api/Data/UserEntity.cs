using System;
using System.Collections.Generic;

namespace SaszetApp.Api.Data
{
    public class UserEntity
    {
        public string Id { get; set; } = string.Empty; // Keycloak Subject UUID
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<CatEntity> Cats { get; set; } = new List<CatEntity>();
    }
}
