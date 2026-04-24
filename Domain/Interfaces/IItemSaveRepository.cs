using Domain.Contracts.Wardrobe;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IItemSaveRepository
    {
        Task DeleteSaveItem(SavedItem savedItem);
        Task<IEnumerable<SavedItem>> GetMySaveItems(int accId);
        Task<List<int>> GetSavedItemIdsByAccountIdAsync(int accId);
        Task<SavedItem?> GetSaveItem(int itemId, int accId);
        Task SaveItem(SavedItem savedItem);

        Task<List<PublicWardrobeItemDto>> GetMySavedItemDtosAsync(int accId);
    }
}