using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SaszetApp.Api.DTOs
{
    public class MultiVlmResponseContract
    {
        [JsonPropertyName("products")]
        public List<VlmResponseContract> Products { get; set; } = new();
    }
}
