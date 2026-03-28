using Microsoft.AspNetCore.Http;
using Pgvector;
using Services.Request.ItemReq;


namespace Services.AI
{
    public interface IAiService
    {
        Task<Vector> GetEmbeddingFromPhotoAsync(ProductUploadDto dto,string imageUrl);

        Task<Vector> GetTextEmbeddingAsync(string prompt);
    }
}
