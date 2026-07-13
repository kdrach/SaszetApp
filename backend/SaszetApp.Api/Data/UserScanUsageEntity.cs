using System;
using System.ComponentModel.DataAnnotations;

namespace SaszetApp.Api.Data
{
    public class UserScanUsageEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; }
    }
}
