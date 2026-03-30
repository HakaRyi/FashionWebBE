using Services.Request.ItemReq;
using Services.Request.ItemRequest;
using Services.Response.ItemResp;

namespace Services.Implements.Items
{
    public interface IItemService
    {
        Task<IEnumerable<ItemResponseDto>> GetAllItemsAsync();
        Task<ItemResponseDto?> GetItemByIdAsync(int id);
        Task<ItemResponseDto> CreateFashionItemAsync(Request.ItemReq.ProductUploadDto dto, int accountId);
        Task<List<ItemResponseDto>> GetRecommendationsAsync(string prompt);
        Task<IEnumerable<ItemResponseDto>> GetMyItemsAsync(int accountid);
        Task UpdateItem(int itemId, UpdateItemRequest request);
        Task DeleteItem(int itemId);
        Task<List<ItemResponseDto>> GetSmartRecommendationsAsync(SmartRecommendationRequestDto request);

    }
}
