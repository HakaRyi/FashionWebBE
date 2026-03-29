using Services.Request.WardrobeReq;
using Services.Response.ItemResp;
using Services.Response.WardrobeResp;

namespace Services.Implements.Wardrobe
{
    public interface IWardrobeService
    {
        Task<List<WardrobeResponse>> GetAll();
        Task<WardrobeResponse> GetById(int id);
        Task<int> Create(WardrobeRequest request);
        Task<List<ItemDto>> GetMyWardrobeItemsAsync(int accountId);
    }
}
