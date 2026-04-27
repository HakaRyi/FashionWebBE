using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class PhysicalProfileRepository : IPhysicalProfileRepository
    {
        private readonly FashionDbContext _db;

        public PhysicalProfileRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(PhysicalProfile profile)
        {
            var currentProfiles = await _db.PhysicalProfiles
                .Where(p => p.AccountId == profile.AccountId && p.IsCurrent)
                .ToListAsync();

            foreach (var cp in currentProfiles)
            {
                cp.IsCurrent = false;
            }

            profile.RecordedAt = DateTime.UtcNow;
            profile.IsCurrent = true;
            await _db.PhysicalProfiles.AddAsync(profile);
            await _db.SaveChangesAsync();
        }

        public async Task<PhysicalProfile?> GetCurrentByAccountIdAsync(int accountId)
        {
            return await _db.PhysicalProfiles
                .FirstOrDefaultAsync(p => p.AccountId == accountId && p.IsCurrent);
        }

        public async Task<List<PhysicalProfile>> GetHistoryByAccountIdAsync(int accountId)
        {
            return await _db.PhysicalProfiles
                .Where(p => p.AccountId == accountId)
                .OrderByDescending(p => p.RecordedAt)
                .ToListAsync();
        }
    }
}
