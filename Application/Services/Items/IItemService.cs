using Abp.Net.Mail;
using Application.Request.ItemReq;
using Application.Request.ItemRequest;
using Application.Response.ItemResp;
using Domain.Dto.Wardrobe;

namespace Application.Services.Items
{
    public interface IItemService
    {
        Task<IEnumerable<ItemResponseDto>> GetAllItemsAsync();
        Task<ItemResponseDto?> GetItemByIdAsync(int id);
        Task<PublicWardrobeItemDetailDto?> GetPublicItemDetailAsync(int itemId);
        Task<ItemResponseDto> CreateFashionItemAsync(ProductUploadDto dto, int accountId);
        Task<List<ItemResponseDto>> GetRecommendationsAsync(string prompt);
        Task<List<ItemResponseDto>> GetSmartRecommendationsAsync(SmartRecommendationRequestDto request);
        Task<(IEnumerable<ItemResponseDto> Items, int TotalCount)> GetMyItemsAsync(int accountId, int page, int pageSize, string? search);
        Task UpdateItemAsync(int itemId, UpdateItemRequest request);
        Task DeleteItemAsync(int itemId);
        Task<IEnumerable<ItemResponseDto>> GetAllMyItemsAsync(int accountId);
    }
}