using Microsoft.AspNetCore.Http;

namespace Application.Services.TryOn
{
    public interface ITryOnService
    {
        Task<Stream> ProcessTryOnAsync(IFormFile modelImage, IFormFile clothImage, int? category);
    }
}
