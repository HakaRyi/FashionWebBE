using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.SystemSettingRepos
{
    public class SystemSettingRepository : ISystemSettingRepository
    {
        private readonly FashionDbContext _context;

        public SystemSettingRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetStringValueAsync(string key, string defaultValue)
        {
            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == key);
            return setting?.SettingValue ?? defaultValue;
        }

        public async Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue)
        {
            var val = await GetStringValueAsync(key, defaultValue.ToString());
            return decimal.TryParse(val, out decimal result) ? result : defaultValue;
        }

        public async Task<int> GetIntValueAsync(string key, int defaultValue)
        {
            var val = await GetStringValueAsync(key, defaultValue.ToString());
            return int.TryParse(val, out int result) ? result : defaultValue;
        }

        public async Task<double> GetDoubleValueAsync(string key, double defaultValue)
        {
            var val = await GetStringValueAsync(key, defaultValue.ToString());
            return double.TryParse(val, out double result) ? result : defaultValue;
        }

        public async Task<SystemSetting?> GetByKeyAsync(string key)
        => await _context.SystemSettings.FindAsync(key);

        public async Task<List<SystemSetting>> GetAllAsync()
            => await _context.SystemSettings.ToListAsync();

        public async Task AddAsync(SystemSetting setting)
        {
            _context.SystemSettings.Add(setting);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SystemSetting setting)
        {
            setting.UpdatedAt = DateTime.Now;
            _context.SystemSettings.Update(setting);
            await _context.SaveChangesAsync();
        }
    }
}
