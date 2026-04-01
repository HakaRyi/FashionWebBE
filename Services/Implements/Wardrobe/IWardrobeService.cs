using Repositories.Dto.Wardrobe;
using Services.Request.WardrobeReq;
using Services.Response.ItemResp;
using Services.Response.WardrobeResp;

namespace Services.Implements.Wardrobe
{
    public interface IWardrobeService
    {
        Task<int> CreateAsync(WardrobeRequest request);
        Task<List<WardrobeResponse>> GetAllAsync();
        Task<WardrobeResponse?> GetByAccountIdAsync(int accountId);
        Task<List<ItemDto>> GetMyWardrobeItemsAsync(int accountId);
        Task<PublicProfileDto?> GetPublicProfileAsync(int accountId);
        Task<PublicWardrobeResponseDto?> GetPublicWardrobeAsync(int accountId, int page, int pageSize);
    }
}