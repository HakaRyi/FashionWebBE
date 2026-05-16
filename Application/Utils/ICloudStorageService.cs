using Microsoft.AspNetCore.Http;

namespace Application.Utils
{
    public interface ICloudStorageService
    {
        Task<string> UploadImageAsync(IFormFile file);

        Task<string> UploadImageFromStreamAsync(Stream stream, string fileName);

        Task DeleteImageAsync(string imageUrl);
    }
}
