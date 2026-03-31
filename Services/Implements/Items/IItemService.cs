using Services.Request.ItemReq;
using Services.Request.ItemRequest;
using Services.Response.ItemResp;

namespace Services.Implements.Items
{
    public interface IItemService
    {
        Task<IEnumerable<ItemResponseDto>> GetAllItemsAsync();
        Task<ItemResponseDto?> GetItemByIdAsync(int id);
        Task<ItemResponseDto> CreateFashionItemAsync(ProductUploadDto dto, int accountId);
        Task<List<ItemResponseDto>> GetRecommendationsAsync(string prompt);
        Task<List<ItemResponseDto>> GetSmartRecommendationsAsync(SmartRecommendationRequestDto request);
        Task<IEnumerable<ItemResponseDto>> GetMyItemsAsync(int accountId);
        Task UpdateItemAsync(int itemId, UpdateItemRequest request);
        Task DeleteItemAsync(int itemId);
    }
}