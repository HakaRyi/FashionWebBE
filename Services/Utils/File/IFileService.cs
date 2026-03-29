using Microsoft.AspNetCore.Http;

namespace Services.Utils.File
{
    public interface IFileService
    {
        Task<string> UploadAsync(IFormFile file);

        //Task<bool> DeleteAsync(string publicId);
    }
}
