using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SaszetApp.Api.DTOs
{
    public class VlmResponseContract
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("pros")]
        public List<string> Pros { get; set; } = new();

        [JsonPropertyName("cons")]
        public List<string> Cons { get; set; } = new();

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("extractedIngredients")]
        public string ExtractedIngredients { get; set; } = string.Empty;
    }
}
