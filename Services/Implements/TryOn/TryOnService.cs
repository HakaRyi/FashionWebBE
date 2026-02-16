using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Services.Request.TryOn;
using Services.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.TryOn
{
    public class TryOnService : ITryOnService
    {
        private readonly HttpClient _httpClient;

        public TryOnService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5000/");

            _httpClient.Timeout = TimeSpan.FromMinutes(15);
        }

        public async Task<Stream> ProcessTryOnAsync(IFormFile modelImage, IFormFile clothImage)
        {
            using var content = new MultipartFormDataContent();

            // Đóng gói ảnh người mẫu
            var modelStream = modelImage.OpenReadStream();
            var modelContent = new StreamContent(modelStream);
            modelContent.Headers.ContentType = new MediaTypeHeaderValue(modelImage.ContentType);
            content.Add(modelContent, "model_image", modelImage.FileName);

            // Đóng gói ảnh quần áo
            var clothStream = clothImage.OpenReadStream();
            var clothContent = new StreamContent(clothStream);
            clothContent.Headers.ContentType = new MediaTypeHeaderValue(clothImage.ContentType);
            content.Add(clothContent, "cloth_image", clothImage.FileName);

            var response = await _httpClient.PostAsync("process-flow", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Python Local Error: {error}");
            }
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
