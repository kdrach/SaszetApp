using System.ComponentModel.DataAnnotations;

namespace SaszetApp.Api.Data
{
    public class SystemSettingEntity
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
