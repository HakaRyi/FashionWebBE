namespace Application.Utils.AIDectection
{
    public interface IAIDetectionService
    {
        Task<bool> DetectFashionItemsAsync(string imageUrl);
    }
}
