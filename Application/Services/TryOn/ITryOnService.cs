using Application.Response.TryOn;
using Microsoft.AspNetCore.Http;

namespace Application.Services.TryOn
{
    public interface ITryOnService
    {
        Task<TryOnInfoResponse> GetTryOnInfoAsync();
        Task<Stream> ProcessTryOnAsync(IFormFile modelImage, IFormFile clothImage, int? category);

    }
}