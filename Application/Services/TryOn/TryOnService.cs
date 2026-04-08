using Application.Response.AiResp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Application.Services.TryOn
{
    public class TryOnService : ITryOnService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ootdUrl;
        private readonly string _aiPredictUrl;

        public TryOnService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(15);

            _ootdUrl = config["AISettings:OOTDUrl"]
                ?? throw new Exception("Thiếu cấu hình AISettings:OOTDUrl");

            _aiPredictUrl = config["AISettings:Fashin_PredictionUrl"]
                ?? "https://habenular-unrigidly-fidelia.ngrok-free.dev/predict";
        }

        public async Task<Stream> ProcessTryOnAsync(IFormFile modelImage, IFormFile clothImage, int? category)
        {
            int finalCategory = category ?? await GetClothCategoryAsync(clothImage);

            using var content = new MultipartFormDataContent();

            var modelStream = new MemoryStream();
            await modelImage.CopyToAsync(modelStream);
            modelStream.Position = 0;

            var modelContent = new StreamContent(modelStream);
            modelContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(modelImage.ContentType)
                    ? "application/octet-stream"
                    : modelImage.ContentType
            );
            content.Add(modelContent, "model_image", modelImage.FileName);

            var clothStream = new MemoryStream();
            await clothImage.CopyToAsync(clothStream);
            clothStream.Position = 0;

            var clothContent = new StreamContent(clothStream);
            clothContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(clothImage.ContentType)
                    ? "application/octet-stream"
                    : clothImage.ContentType
            );
            content.Add(clothContent, "cloth_image", clothImage.FileName);

            content.Add(new StringContent(finalCategory.ToString()), "category");

            var response = await _httpClient.PostAsync(_ootdUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Python OOTD Error: {error}");
            }

            return await response.Content.ReadAsStreamAsync();
        }

        private async Task<int> GetClothCategoryAsync(IFormFile clothImage)
        {
            using var content = new MultipartFormDataContent();

            using var memoryStream = new MemoryStream();
            await clothImage.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var imageContent = new ByteArrayContent(fileBytes);
            content.Add(imageContent, "file", clothImage.FileName);

            var response = await _httpClient.PostAsync(_aiPredictUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var prediction = JsonSerializer.Deserialize<AIFashionDetectReponse>(jsonString);

            if (prediction == null || !prediction.IsClothing)
            {
                throw new Exception("Hình ảnh không phải là trang phục hợp lệ.");
            }

            return MapLabelToCategory(prediction.Label?.ToLower() ?? "");
        }

        private int MapLabelToCategory(string label)
        {
            var lowerBodyKeywords = new[] { "skirt", "miniskirt", "pants", "jeans", "shorts", "trousers", "hoopskirt", "swimming_trunks" };
            if (lowerBodyKeywords.Any(k => label.Contains(k)))
            {
                return 1;
            }

            var dressKeywords = new[] { "dress", "gown", "robe", "kimono" };
            if (dressKeywords.Any(k => label.Contains(k)))
            {
                return 2;
            }

            return 0;
        }
    }
}