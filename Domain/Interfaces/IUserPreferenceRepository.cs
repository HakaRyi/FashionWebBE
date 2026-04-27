using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUserPreferenceRepository
    {
        Task AddRangeAsync(List<UserPreference> preferences);
        Task<List<UserPreference>> GetByAccountIdAsync(int accountId);
        Task ReplacePreferencesAsync(int accountId, string type, List<string> newValues);
    }
}
