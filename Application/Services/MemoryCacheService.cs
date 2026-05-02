using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task<T?> GetDataAsync<T>(string key)
        {
            _memoryCache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public Task SetDataAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null)
        {
            var options = new MemoryCacheEntryOptions();
            if (absoluteExpireTime.HasValue)
            {
                options.SetAbsoluteExpiration(absoluteExpireTime.Value);
            }

            _memoryCache.Set(key, data, options);
            return Task.CompletedTask;
        }

        public Task RemoveDataAsync(string key)
        {
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? absoluteExpireTime = null)
        {
            if (_memoryCache.TryGetValue(key, out T? value))
            {
                return value;
            }
            value = await factory();

            if (value != null)
            {
                await SetDataAsync(key, value, absoluteExpireTime);
            }

            return value;
        }
    }
}
