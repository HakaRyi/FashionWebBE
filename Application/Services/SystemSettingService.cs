using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
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

        public async Task<List<SystemSetting>> GetAllSettingsAsync() => await _repository.GetAllAsync();

        public async Task<SystemSetting?> GetByKeyAsync(string key) => await _repository.GetByKeyAsync(key);

        public async Task<object> GetEventCreationMetadataAsync()
        {
            var settings = await _repository.GetAllAsync();

            return new
            {
                ExpertRules = new
                {
                    MinRequired = int.Parse(GetSettingValue(settings, "MIN_EXPERTS_PER_EVENT", "2")),
                },

                FinancialRules = new
                {
                    FeePercentage = double.Parse(GetSettingValue(settings, "EVENT_FEE_PERCENTAGE", "5")),
                    MinFee = double.Parse(GetSettingValue(settings, "EVENT_MIN_FEE", "0")),
                    Currency = "VND"
                },
            };
        }

        private string GetSettingValue(IEnumerable<SystemSetting> settings, string key, string defaultValue)
        {
            return settings.FirstOrDefault(s => s.SettingKey == key)?.SettingValue ?? defaultValue;
        }
    }
}
