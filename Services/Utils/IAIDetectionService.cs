namespace Services.Utils
{
    public interface IAIDetectionService
    {
        Task<bool> DetectFashionItemsAsync(string imageUrl);
    }
}
