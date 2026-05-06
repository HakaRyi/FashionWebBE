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
    public class CollectionRepository : ICollectionRepository
    {
        private readonly FashionDbContext _context;
        public CollectionRepository(FashionDbContext context) => _context = context;
        public async Task CreateAsync(Collection collection)
        {
            await _context.Collections.AddAsync(collection);
        }

        public async Task DeleteAsync(Collection collection)
        {
            _context.Collections.Remove(collection);
        }

        public async Task<List<Collection>> GetByAccountIdAsync(int accountId)
        {
            return await _context.Collections
                .Include(c => c.CollectionItems)
                    .ThenInclude(ci => ci.Item)
                        .ThenInclude(i => i.Images)
                .Include(c => c.CollectionItems)
                    .ThenInclude(ci => ci.Item)
                        .ThenInclude(i => i.Wardrobe)
                            .ThenInclude(w => w.Account)
                .Where(c => c.AccountId == accountId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Collection?> GetByIdAsync(int id, int accountId)
        {
            return await _context.Collections
            .Include(c => c.CollectionItems)
            .FirstOrDefaultAsync(c => c.CollectionId == id && c.AccountId == accountId);
        }

        public async Task UpdateAsync(Collection collection)
        {
            _context.Collections.Update(collection);
        }
    }
}
