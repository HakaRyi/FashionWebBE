using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Response.ItemResp;

namespace Application.Services.ItemSaveImp
{
    public interface IItemSaveService
    {
        Task SaveItem(int itemId);
        Task DeleteSaveItem(int itemId);
        Task<IEnumerable<ItemResponseDto>> GetMySaveItems();
    }
}
