using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.SystemSettingRepos
{
    public interface ISystemSettingRepository
    {
        Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue);
        Task<int> GetIntValueAsync(string key, int defaultValue);
        Task<string> GetStringValueAsync(string key, string defaultValue);
        Task<double> GetDoubleValueAsync(string key, double defaultValue);
        Task<SystemSetting?> GetByKeyAsync(string key);
        Task<List<SystemSetting>> GetAllAsync();
        Task UpdateAsync(SystemSetting setting);
        Task AddAsync(SystemSetting setting);
    }
}
