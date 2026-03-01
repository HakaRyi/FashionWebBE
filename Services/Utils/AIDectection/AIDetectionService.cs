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
            _apiUrl = config["AISettings:ApiUrl"]!;
        }

        public async Task<bool> DetectFashionItemsAsync(string imageUrl)
        {
            Console.WriteLine($"[AIDetection] Sending URL: {imageUrl} to {_apiUrl}");

            try
            {
                var payload = new { image_url = imageUrl };

                var response = await _httpClient.PostAsJsonAsync(_apiUrl, payload);

                Console.WriteLine($"[AIDetection] Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AIDetection] AI API Error Body: {errorBody}");
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<AIFashionDetectReponse>();

                Console.WriteLine($"[AIDetection] IsFashion: {result?.IsFashion}");

                return result?.IsFashion ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIDetection] Exception calling AI: {ex.Message}");
                return false;
            }
        }
    }
}