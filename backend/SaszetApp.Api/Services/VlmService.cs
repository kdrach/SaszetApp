using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
using SaszetApp.Api.Data;
using SaszetApp.Api.DTOs;
using SaszetApp.Api.Models;

namespace SaszetApp.Api.Services
{
    public class VlmService : IVlmService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public VlmService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && ((int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.TooManyRequests))
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetryAsync: (outcome, timespan, retryAttempt, context) =>
                    {
                        outcome.Result?.Dispose();
                        return Task.CompletedTask;
                    });
        }

        public async Task<VlmResponseContract> AnalyzeProductAsync(string providerName, string modelName, string apiKey, string query, string language, CancellationToken cancellationToken)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var systemPrompt = $"You are a pet food analyst. The user will provide a product name or label text. " +
                               $"Analyze it and return ONLY a JSON object exactly matching this structure: " +
                                $"{{\"productName\": \"...\", \"rating\": 8, \"pros\": [\"...\"], \"cons\": [\"...\"], \"summary\": \"...\", \"extractedIngredients\": \"...\"}}. " +
                               $"All text values (except productName) MUST be in the '{language}' language.";

            string jsonResponse = await CallProviderAsync(providerName, modelName, apiKey, systemPrompt, query, cancellationToken);

            var match = Regex.Match(jsonResponse, @"(?is)```(?:json)?\s*(.*?)\s*```");
            if (match.Success) 
            {
                jsonResponse = match.Groups[1].Value;
            }
            
            jsonResponse = jsonResponse.Trim();

            var vlmResponse = JsonSerializer.Deserialize<VlmResponseContract>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (vlmResponse == null) throw new InvalidOperationException("Failed to parse VLM response.");

            return Sanitize(vlmResponse);
        }

        public async Task<VlmResponseContract> AnalyzeImageAsync(string providerName, string modelName, string apiKey, string base64Image, string mimeType, string language, CancellationToken cancellationToken)
        {
            string systemPrompt = $"You are a pet food analyst. The user will provide a photo of the ingredients label. " +
                               $"Analyze the ingredients accurately. Return ONLY a JSON object exactly matching this structure: " +
                                $"{{\"productName\": \"...\", \"rating\": 8, \"pros\": [\"...\"], \"cons\": [\"...\"], \"summary\": \"...\", \"extractedIngredients\": \"...\"}}. " +
                               $"If the image does not contain pet food packaging or an ingredients list, return exactly: {{\"errorCode\": \"NO_PET_FOOD_FOUND\"}}. " +
                               $"All text values (except productName) MUST be in the '{language}' language.";

            string jsonResponse = await CallProviderForImageAsync(providerName, modelName, apiKey, systemPrompt, base64Image, mimeType, cancellationToken);

            var match = Regex.Match(jsonResponse, @"(?is)```(?:json)?\s*(.*?)\s*```");
            if (match.Success) jsonResponse = match.Groups[1].Value;
            jsonResponse = jsonResponse.Trim();

            if (jsonResponse.Contains("\"errorCode\"") && jsonResponse.Contains("NO_PET_FOOD_FOUND"))
            {
                throw new InvalidOperationException("NO_PET_FOOD_FOUND");
            }

            var vlmResponse = JsonSerializer.Deserialize<VlmResponseContract>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (vlmResponse == null) throw new InvalidOperationException("Failed to parse VLM response.");

            return Sanitize(vlmResponse);
        }

        private VlmResponseContract Sanitize(VlmResponseContract response)
        {
            if (response == null) return null;
            var sanitizer = new Ganss.Xss.HtmlSanitizer();
            return new VlmResponseContract
            {
                ProductName = response.ProductName != null ? sanitizer.Sanitize(response.ProductName) : null,
                Rating = response.Rating,
                Pros = response.Pros?.Select(p => p != null ? sanitizer.Sanitize(p) : null).ToList(),
                Cons = response.Cons?.Select(c => c != null ? sanitizer.Sanitize(c) : null).ToList(),
                Summary = response.Summary != null ? sanitizer.Sanitize(response.Summary) : null,
                ExtractedIngredients = response.ExtractedIngredients != null ? sanitizer.Sanitize(response.ExtractedIngredients) : null
            };
        }

        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> action, CancellationToken cancellationToken)
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var res = await action();
                if (!res.IsSuccessStatusCode && ((int)res.StatusCode < 500 && res.StatusCode != HttpStatusCode.TooManyRequests))
                {
                    var errorContent = await res.Content.ReadAsStringAsync(cancellationToken);
                    res.Dispose();
                    throw new InvalidOperationException($"API error: {res.StatusCode} - {errorContent}");
                }
                return res;
            });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
                throw new InvalidOperationException($"API error: {response.StatusCode} - {errorContent}");
            }

            return response;
        }

        private async Task<string> CallProviderAsync(string provider, string model, string apiKey, string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration[$"LlmEndpoints:{provider}"];
            
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

                var url = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1/chat/completions" : $"{baseUrl.TrimEnd('/')}/chat/completions";
                using var response = await ExecuteWithRetryAsync(() => client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), cancellationToken), cancellationToken);

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
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

                var url = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.anthropic.com/v1/messages" : $"{baseUrl.TrimEnd('/')}/messages";
                using var response = await ExecuteWithRetryAsync(() => client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), cancellationToken), cancellationToken);

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";
            }
            else if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

                var requestBody = new
                {
                    systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                var actualModel = model.StartsWith("models/") ? model.Substring("models/".Length) : model;
                var encodedModel = System.Net.WebUtility.UrlEncode(actualModel);
                
                var url = string.IsNullOrWhiteSpace(baseUrl) ? $"https://generativelanguage.googleapis.com/v1beta/models/{encodedModel}:generateContent" : $"{baseUrl.TrimEnd('/')}/models/{encodedModel}:generateContent";
                using var response = await ExecuteWithRetryAsync(() => client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), cancellationToken), cancellationToken);

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";
            }

            throw new NotSupportedException($"Provider {provider} is not supported.");
        }

        private async Task<string> CallProviderForImageAsync(string provider, string model, string apiKey, string systemPrompt, string base64Image, string mimeType, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration[$"LlmEndpoints:{provider}"];
            
            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var requestBody = new
                {
                    model = model,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = new object[] {
                            new { type = "text", text = "Analyze this image." },
                            new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64Image}" } }
                        } }
                    },
                    response_format = new { type = "json_object" }
                };

                var url = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1/chat/completions" : $"{baseUrl.TrimEnd('/')}/chat/completions";
                using var response = await ExecuteWithRetryAsync(() => client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), cancellationToken), cancellationToken);

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
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
                    messages = new object[]
                    {
                        new { role = "user", content = new object[] {
                            new { type = "text", text = "Analyze this image." },
                            new { type = "image", source = new { type = "base64", media_type = mimeType, data = base64Image } }
                        } }
                    }
                };

                var url = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.anthropic.com/v1/messages" : $"{baseUrl.TrimEnd('/')}/messages";
                using var response = await ExecuteWithRetryAsync(() => client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), cancellationToken), cancellationToken);

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";
            }
            else if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

                var requestBody = new
                {
                    systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                    contents = new[] { new { role = "user", parts = new object[] { 
                        new { text = "Analyze this image." },
                        new { inlineData = new { mimeType = mimeType, data = base64Image } }
                    } } },
                    generationConfig = new { responseMimeType = "application/json" }
                };
                
                var actualModel = model.StartsWith("models/") ? model.Substring("models/".Length) : model;
                var encodedModel = System.Net.WebUtility.UrlEncode(actualModel);
                
                var url = string.IsNullOrWhiteSpace(baseUrl) ? $"https://generativelanguage.googleapis.com/v1beta/models/{encodedModel}:generateContent" : $"{baseUrl.TrimEnd('/')}/models/{encodedModel}:generateContent";
                using var response = await ExecuteWithRetryAsync(() => client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), cancellationToken), cancellationToken);

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";
            }

            throw new NotSupportedException($"Provider {provider} is not supported.");
        }
        public async Task TestConnectionAsync(string providerName, string modelName, string apiKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) 
                throw new InvalidOperationException("Invalid or missing API key.");
                
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration[$"LlmEndpoints:{providerName}"];
            
            if (providerName == "Gemini" && modelName.StartsWith("models/"))
            {
                modelName = modelName.Substring("models/".Length);
            }
            var encodedModel = System.Net.WebUtility.UrlEncode(modelName);

            if (providerName == "Anthropic")
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                
                var requestBody = new
                {
                    model = encodedModel,
                    max_tokens = 1,
                    messages = new[] { new { role = "user", content = "Test" } }
                };
                var url = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.anthropic.com/v1/messages" : $"{baseUrl.TrimEnd('/')}/messages";
                using var response = await ExecuteWithRetryAsync(() => client.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), cancellationToken), cancellationToken);
            }
            else if (providerName == "Gemini")
            {
                client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
                var url = string.IsNullOrWhiteSpace(baseUrl) ? $"https://generativelanguage.googleapis.com/v1beta/models/{encodedModel}" : $"{baseUrl.TrimEnd('/')}/models/{encodedModel}";
                using var response = await ExecuteWithRetryAsync(() => client.GetAsync(url, cancellationToken), cancellationToken);
            }
            else
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var url = string.IsNullOrWhiteSpace(baseUrl) ? $"https://api.openai.com/v1/models/{encodedModel}" : $"{baseUrl.TrimEnd('/')}/models/{encodedModel}";
                using var response = await ExecuteWithRetryAsync(() => client.GetAsync(url, cancellationToken), cancellationToken);
            }
        }
    }
}
