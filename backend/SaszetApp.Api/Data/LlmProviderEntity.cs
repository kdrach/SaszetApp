using System;

namespace SaszetApp.Api.Data
{
    public class LlmProviderEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string ProviderName { get; set; } = string.Empty;
        
        public string ModelName { get; set; } = string.Empty;
        
        public string EncryptedApiKey { get; set; } = string.Empty;
        
        public bool IsPrimary { get; set; }
        
        public bool IsActive { get; set; }
    }
}
