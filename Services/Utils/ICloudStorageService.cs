using Microsoft.AspNetCore.Http;

namespace Services.Utils
{
    public interface ICloudStorageService
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<string> UploadImageFromStreamAsync(Stream stream, string fileName);
    }
}
