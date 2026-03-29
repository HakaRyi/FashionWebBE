using Microsoft.Extensions.Configuration;
using Repositories.Repos.AI;
using System.Net.Http.Json;

namespace Services.Utils.AIDectection
{
    public class AIDetectionService : IAIDetectionService
    {
        private readonly string _apiUrl;
        private readonly HttpClient _httpClient;

        public AIDetectionService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiUrl = config["AISettings:ApiUrl"]!
                ?? throw new InvalidOperationException("AISettings:ApiUrl is not configured.");
        }

        public async Task<bool> DetectFashionItemsAsync(string imageUrl)
        {
            Console.WriteLine($"[AIDetection] Sending URL: {imageUrl} to {_apiUrl}");

            var payload = new { image_url = imageUrl };

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync(_apiUrl, payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIDetection] Exception calling AI: {ex.Message}");
                throw new Exception("AI detection service is unavailable.", ex);
            }

            Console.WriteLine($"[AIDetection] Response Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[AIDetection] AI API Error Body: {errorBody}");

                throw new Exception(
                    $"AI API returned non-success status code: {(int)response.StatusCode} {response.StatusCode}");
            }

            AIFashionDetectReponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<AIFashionDetectReponse>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIDetection] Failed to deserialize AI response: {ex.Message}");
                throw new Exception("Failed to parse AI detection response.", ex);
            }

            if (result == null)
            {
                throw new Exception("AI detection response body is null.");
            }

            Console.WriteLine($"[AIDetection] IsFashion: {result.IsFashion}");

            return result.IsFashion;
        }
    }
}