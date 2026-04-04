using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISystemSettingService
    {
        Task<string> GetValueAsync(string key, string defaultValue = "");
        Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0);
        Task UpdateSettingAsync(string key, string value);
        Task<(decimal Percentage, decimal MinFee)> GetEventFeeConfigAsync();
        Task<List<SystemSetting>> GetAllSettingsAsync();
        Task<SystemSetting?> GetByKeyAsync(string key);
        Task<object> GetEventCreationMetadataAsync();
    }
}
