using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pgvector;
using Services.Response.AiResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;


namespace Services.AI
{
    public class AiService: IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _baseUrl = config["AiServer:BaseUrl"] ?? throw new Exception("AiServer:BaseUrl is missing in config");
        }

        protected async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI Service Error ({response.StatusCode}): {error}");
            }
        }

        public async Task<Vector> GetEmbeddingFromPhotoAsync(IFormFile file, string description)
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();

            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            content.Add(streamContent, "file", file.FileName);
            content.Add(new StringContent(description ?? ""), "description");

            var response = await _httpClient.PostAsync($"{_baseUrl}/get-embedding", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VectorApiResponse>();

            if (result?.Embedding == null || result.Embedding.Length != 768)
                throw new Exception("❌ Invalid embedding dimension received from AI.");

            return new Vector(result.Embedding);
        }

        public async Task<Vector> GetTextEmbeddingAsync(string prompt)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/text-to-embedding?query={Uri.EscapeDataString(prompt)}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<VectorApiResponse>();
            return new Vector(result?.Embedding ?? Array.Empty<float>());
        }
    }
}
