using Microsoft.AspNetCore.Http;

namespace Application.Utils.File
{
    public interface IFileService
    {
        Task<string> UploadAsync(IFormFile file);

        //Task<bool> DeleteAsync(string publicId);
    }
}
