using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace Services.Implements.TryOn
{
    public class TryOnService : ITryOnService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ootdUrl;

        public TryOnService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(15);

            _ootdUrl = config["AISettings:OOTDUrl"]
                ?? throw new Exception("Thiếu cấu hình AISettings:OOTDUrl");
        }

        public async Task<Stream> ProcessTryOnAsync(IFormFile modelImage, IFormFile clothImage)
        {
            using var content = new MultipartFormDataContent();

            var modelStream = modelImage.OpenReadStream();
            var modelContent = new StreamContent(modelStream);
            modelContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(modelImage.ContentType)
                    ? "application/octet-stream"
                    : modelImage.ContentType
            );
            content.Add(modelContent, "model_image", modelImage.FileName);

            var clothStream = clothImage.OpenReadStream();
            var clothContent = new StreamContent(clothStream);
            clothContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(clothImage.ContentType)
                    ? "application/octet-stream"
                    : clothImage.ContentType
            );
            content.Add(clothContent, "cloth_image", clothImage.FileName);

            content.Add(new StringContent("0"), "category");

            var response = await _httpClient.PostAsync(_ootdUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Python OOTD Error: {error}");
            }

            return await response.Content.ReadAsStreamAsync();
        }
    }
}