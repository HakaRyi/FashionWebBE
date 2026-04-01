using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Pgvector;
using Services.Request.ItemReq;
using Services.Response.AiResp;
using System.Net.Http.Json;



namespace Services.AI
{
    public class AiService : IAiService
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


        public async Task<Vector> GetEmbeddingFromPhotoAsync(ProductUploadDto dto, string imageUrl)
        {
            if (dto.PrimaryImageUrl == null) throw new ArgumentException("File is required");

            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(imageUrl), "image_url");

            void AddField(string key, string? value)
            {
                content.Add(new StringContent(value ?? "unknown"), key);
            }

            AddField("item", dto.ItemType ?? dto.ItemName);
            AddField("category", dto.Category?.ToString());
            AddField("sub_category", dto.SubCategory);
            AddField("gender", dto.Gender);
            AddField("main_color", dto.MainColor);
            AddField("sub_color", dto.SubColor);
            AddField("material", dto.Material);
            AddField("style", dto.Style);
            AddField("pattern", dto.Pattern);
            AddField("sleeve_length", dto.SleeveLength);
            AddField("length", dto.Length);
            AddField("neckline", dto.Neckline);
            AddField("fit", dto.Fit);
            AddField("description", dto.Description);

            var response = await _httpClient.PostAsync($"{_baseUrl}/get-embedding", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"❌ AI API Error: {response.StatusCode} - {errorBody}");
            }

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
