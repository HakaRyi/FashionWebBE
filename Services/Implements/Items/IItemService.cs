using Services.Request.ItemReq;
using Services.Response.ItemResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.Items
{
    public interface IItemService
    {
        Task<IEnumerable<ItemResponseDto>> GetAllItemsAsync();
        Task<ItemResponseDto?> GetItemByIdAsync(int id);
        Task<ItemResponseDto> CreateFashionItemAsync(ProductUploadDto dto);
        Task<List<ItemResponseDto>> GetRecommendationsAsync(string prompt);
        Task<List<ItemResponseDto>> GetSmartRecommendationsAsync(SmartRecommendationRequestDto request);
    }
}
