using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.SystemSettingImp
{
    public interface ISystemSettingService
    {
        Task<string> GetValueAsync(string key, string defaultValue = "");
        Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0);
        Task UpdateSettingAsync(string key, string value);
        Task<(decimal Percentage, decimal MinFee)> GetEventFeeConfigAsync();
    }
}
