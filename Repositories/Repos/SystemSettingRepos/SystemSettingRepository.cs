using Microsoft.EntityFrameworkCore;
using Repositories.Data;
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
    }
}
