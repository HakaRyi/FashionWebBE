using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetDataAsync<T>(string key);
        Task SetDataAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null);
        Task RemoveDataAsync(string key);
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? absoluteExpireTime = null);
    }
}
