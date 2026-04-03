using Repositories.Repos.SystemSettingRepos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.SystemSettingImp
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ISystemSettingRepository _repository;
        public SystemSettingService(ISystemSettingRepository repository) => _repository = repository;

        public async Task<string> GetValueAsync(string key, string defaultValue = "")
        {
            var setting = await _repository.GetByKeyAsync(key);
            return setting?.SettingValue ?? defaultValue;
        }

        public async Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0)
        {
            var value = await GetValueAsync(key);
            return decimal.TryParse(value, out var result) ? result : defaultValue;
        }

        public async Task UpdateSettingAsync(string key, string value)
        {
            var setting = await _repository.GetByKeyAsync(key);
            if (setting != null)
            {
                setting.SettingValue = value;
                await _repository.UpdateAsync(setting);
            }
        }

        public async Task<(decimal Percentage, decimal MinFee)> GetEventFeeConfigAsync()
        {
            var percentage = await GetDecimalValueAsync("EventFeePercentage", 5);
            var minFee = await GetDecimalValueAsync("EventMinFee", 10000);
            return (percentage, minFee);
        }
    }
}
