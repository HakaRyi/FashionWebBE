using Microsoft.Extensions.Configuration;
using Repositories.Repos.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Services.Utils.AIDectection
{
    public class AIDetectionService : IAIDetectionService
    {
        private readonly string _pythonPath;
        private readonly string _scriptPath;
        private readonly string _apiUrl;
        private readonly HttpClient _httpClient;

        public AIDetectionService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiUrl = config["AISettings:ApiUrl"]!;
        }

        public async Task<bool> DetectFashionItemsAsync(string imageUrl)
        {
            try
            {
                var payload = new { image_url = imageUrl };

                var response = await _httpClient.PostAsJsonAsync(_apiUrl, payload);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"AI API Error: {response.StatusCode}");
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<AIFashionDetectReponse>();

                return result?.IsFashion ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception calling AI: {ex.Message}");
                return false;
            }
        }
    }
}
