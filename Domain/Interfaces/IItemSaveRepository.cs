using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IItemSaveRepository
    {
        Task<SavedItem> GetSaveItem(int itemId, int accId);
        Task<IEnumerable<SavedItem>> GetMySaveItems(int accId);
        Task SaveItem(SavedItem savedItem);
        Task DeleteSaveItem(SavedItem savedItem);
    }
}
