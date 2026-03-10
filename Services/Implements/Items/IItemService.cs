using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;
using Services.Response.ItemResp;

namespace Services.Implements.Items
{
    public interface IItemService
    {
        Task<IEnumerable<ItemResponseDto>> GetAllItemsAsync();
        Task<ItemResponseDto?> GetItemByIdAsync(int id);
        Task<ItemResponseDto> CreateFashionItemAsync(ProductUploadDto dto, int accountId);
        Task<List<ItemResponseDto>> GetRecommendationsAsync(string prompt);
        Task<IEnumerable<ItemResponseDto>> GetMyItemsAsync(int accountid);
    }
}
