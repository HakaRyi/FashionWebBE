namespace Services.Utils.AIDectection
{
    public interface IAIDetectionService
    {
        Task<bool> DetectFashionItemsAsync(string imageUrl);
    }
}
