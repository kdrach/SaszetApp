using System.ComponentModel.DataAnnotations;

namespace SaszetApp.Api.Data
{
    public class UserScanLimitEntity
    {
        [Key]
        public string UserId { get; set; } = string.Empty;
        public int MaxScans { get; set; }
    }
}
