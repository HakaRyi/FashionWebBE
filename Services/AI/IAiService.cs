using Microsoft.AspNetCore.Http;
using Pgvector;


namespace Services.AI
{
    public interface IAiService
    {
        Task<Vector> GetEmbeddingFromPhotoAsync(IFormFile file, string description);
        Task<Vector> GetTextEmbeddingAsync(string prompt);
    }
}
