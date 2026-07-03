using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SaszetApp.Api.Data;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Models;
using SaszetApp.Api.Services.Mappers;

namespace SaszetApp.Api.Services
{
    public class VlmService : IVlmService
    {
        private readonly AppDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEncryptionService _encryptionService;
        private readonly IPetFoodModelMapper _mapper;

        public VlmService(
            AppDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IEncryptionService encryptionService,
            IPetFoodModelMapper mapper)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _encryptionService = encryptionService;
            _mapper = mapper;
        }

        public async Task<PetFoodItem> AnalyzeProductAsync(string query, string language)
        {
            var providerEntity = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.IsPrimary && p.IsActive);
            if (providerEntity == null)
            {
                throw new InvalidOperationException("No active primary LLM provider configured.");
            }

            var apiKey = _encryptionService.Decrypt(providerEntity.EncryptedApiKey);
            var systemPrompt = $"You are a pet food analyst. The user will provide a product name or label text. " +
                               $"Analyze it and return ONLY a JSON object exactly matching this structure: " +
                               $"{{\"productName\": \"...\", \"rating\": 8, \"pros\": [\"...\"], \"cons\": [\"...\"], \"summary\": \"...\", \"extractedIngredients\": \"...\"}}. " +
                               $"All text values (except productName) MUST be in the '{language}' language.";

            string jsonResponse = await CallProviderAsync(providerEntity.ProviderName, providerEntity.ModelName, apiKey, systemPrompt, query);

            var match = Regex.Match(jsonResponse, @"(?is)```(?:json)?\s*(.*?)\s*```");
            if (match.Success) 
            {
                jsonResponse = match.Groups[1].Value;
            }
            
            jsonResponse = jsonResponse.Trim();

            var vlmResponse = JsonSerializer.Deserialize<VlmResponseContract>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (vlmResponse == null) throw new InvalidOperationException("Failed to parse VLM response.");

            var newEntity = new PetFoodItemEntity
            {
                Id = Guid.NewGuid(),
                EanCode = null, // Or try to parse from query if it's numeric
                ProductName = vlmResponse.ProductName,
                Language = language,
                Rating = vlmResponse.Rating,
                Pros = vlmResponse.Pros,
                Cons = vlmResponse.Cons,
                Summary = vlmResponse.Summary,
                ExtractedIngredients = vlmResponse.ExtractedIngredients,
                CreatedAt = DateTime.UtcNow
            };

            // Heuristic for EAN vs Name
            if (query.All(char.IsDigit) && query.Length >= 8)
            {
                newEntity.EanCode = query;
            }

            _dbContext.PetFoodItems.Add(newEntity);
            await _dbContext.SaveChangesAsync();

            return _mapper.MapToModel(newEntity);
        }

        private async Task<string> CallProviderAsync(string provider, string model, string apiKey, string systemPrompt, string userPrompt)
        {
            var client = _httpClientFactory.CreateClient();
            
            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    response_format = new { type = "json_object" }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}";
            }
            else if (provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                
                var requestBody = new
                {
                    model = model,
                    max_tokens = 1000,
                    system = systemPrompt + " You must return ONLY the raw JSON object.",
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";
            }
            else if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                var requestBody = new
                {
                    systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}", content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";
            }

            throw new NotSupportedException($"Provider {provider} is not supported.");
        }
    }
}
