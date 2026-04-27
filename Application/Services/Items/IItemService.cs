using Application.Request.ItemReq;
using Application.Request.ItemRequest;
using Application.Response.ItemResp;
using Domain.Contracts.Wardrobe;

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

        Task<(IEnumerable<ItemResponseDto> Items, int TotalCount)> GetMyItemsAsync(
            int accountId,
            int page,
            int pageSize,
            string? search);

        Task<IEnumerable<ItemResponseDto>> GetAllMyItemsAsync(int accountId);

        Task UpdateItemAsync(int itemId, UpdateItemRequest request);

        Task DeleteItemAsync(int itemId);

        Task<List<ItemVariantResponseDto>> GetItemVariantsAsync(int itemId);

        Task<ItemVariantResponseDto> CreateItemVariantAsync(
            int itemId,
            CreateItemVariantRequest request);

        Task<ItemVariantResponseDto> UpdateItemVariantAsync(
            int itemVariantId,
            UpdateItemVariantRequest request);

        Task DeleteItemVariantAsync(int itemVariantId);

        Task<ItemCommerceResponseDto> PublishItemForSaleAsync(
            int itemId,
            PublishItemForSaleRequest request);

        Task<ItemCommerceResponseDto> UnpublishItemAsync(int itemId);

        Task<List<PublicWardrobeItemDto>> GetMySavedItemsAsync();

        Task SaveItemAsync(int itemId);

        Task UnsaveItemAsync(int itemId);
    }
}