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
    public class UserPreferenceRepository : IUserPreferenceRepository
    {
        private readonly FashionDbContext _db;

        public UserPreferenceRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task AddRangeAsync(List<UserPreference> preferences)
        {
            await _db.UserPreferences.AddRangeAsync(preferences);
            await _db.SaveChangesAsync();
        }

        public async Task<List<UserPreference>> GetByAccountIdAsync(int accountId)
        {
            return await _db.UserPreferences
                .Where(p => p.AccountId == accountId)
                .ToListAsync();
        }

        public async Task ReplacePreferencesAsync(int accountId, string type, List<string> newValues)
        {
            // Xóa các sở thích cũ của loại này (ví dụ xóa hết các màu cũ để thay bằng danh sách màu mới)
            var oldPrefs = await _db.UserPreferences
                .Where(p => p.AccountId == accountId && p.PreferenceType == type)
                .ToListAsync();

            _db.UserPreferences.RemoveRange(oldPrefs);

            // Thêm mới
            var newPrefs = newValues.Select(v => new UserPreference
            {
                AccountId = accountId,
                PreferenceType = type,
                Value = v
            });

            await _db.UserPreferences.AddRangeAsync(newPrefs);
            await _db.SaveChangesAsync();
        }
    }
}
