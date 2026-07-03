using System;

namespace SaszetApp.Api.Models
{
    public class LlmProvider
    {
        public Guid Id { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string EncryptedApiKey { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
    }
}
