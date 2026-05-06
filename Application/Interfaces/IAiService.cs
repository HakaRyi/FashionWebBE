using Pgvector;
using Application.Request.ItemReq;


namespace Application.Interfaces
{
    public interface IAiService
    {
        Task<Vector> GetEmbeddingFromPhotoAsync(ProductUploadDto dto, string imageUrl);

        Task<Vector> GetTextEmbeddingAsync(string prompt);
    }
}
