using Microsoft.AspNetCore.Http;

namespace Services.Implements.TryOn
{
    public interface ITryOnService
    {
        Task<Stream> ProcessTryOnAsync(IFormFile modelImage, IFormFile clothImage);
    }
}
