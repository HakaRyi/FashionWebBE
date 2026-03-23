using Pgvector;


namespace Services.AI
{
    public interface IAiService
    {
        Task<Vector> GetEmbeddingFromPhotoAsync(string imageUrl, string description);
        Task<Vector> GetTextEmbeddingAsync(string prompt);
    }
}
