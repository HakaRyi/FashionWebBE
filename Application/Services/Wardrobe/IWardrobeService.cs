using Domain.Dto.Wardrobe;
using Application.Request.WardrobeReq;
using Application.Response.ItemResp;
using Application.Response.WardrobeResp;

namespace Application.Services.Wardrobe
{
    public interface IWardrobeService
    {
        Task<int> CreateAsync(WardrobeRequest request);
        Task<List<WardrobeResponse>> GetAllAsync();
        Task<WardrobeResponse?> GetByAccountIdAsync(int accountId);
        Task<List<ItemDto>> GetMyWardrobeItemsAsync();
        Task<PublicProfileDto?> GetPublicProfileAsync(int accountId);
        Task<PublicWardrobeResponseDto?> GetPublicWardrobeAsync(int accountId, int page, int pageSize);
    }
}