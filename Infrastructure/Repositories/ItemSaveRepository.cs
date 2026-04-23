using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ItemSaveRepository : IItemSaveRepository
    {
        private readonly FashionDbContext _db;
        public ItemSaveRepository(FashionDbContext db)
        {
            _db = db;
        }
        public async Task DeleteSaveItem(SavedItem savedItem)
        {
             _db.SavedItems.Remove(savedItem);
        }

        public async Task<IEnumerable<SavedItem>> GetMySaveItems(int accId)
        {
            return await _db.SavedItems
                .Include(s => s.Item)
                    .ThenInclude(i => i.Images)
                .Include(s => s.Item)
                    .ThenInclude(i=>i.Wardrobe)
                        .ThenInclude(w => w.Account)
                .Where(s => s.AccountId == accId)
                .ToListAsync();
        }
        public async Task<List<int>> GetSavedItemIdsByAccountIdAsync(int accId)
        {
            return await _db.SavedItems
                .Where(s => s.AccountId == accId)
                .Select(s => s.ItemId)
                .ToListAsync();
        }

        public async Task<SavedItem> GetSaveItem(int itemId, int accId)
        {
            return await _db.SavedItems
                .Include(s => s.Item)
                    .ThenInclude(i => i.Images)
                .Include(s => s.Account)
                .FirstOrDefaultAsync(s => s.ItemId == itemId && s.AccountId == accId);
        }
        

        public async Task SaveItem(SavedItem savedItem)
        {
            savedItem.SavedAt = DateTime.Now;
            await _db.SavedItems.AddAsync(savedItem);
        }
    }
}
