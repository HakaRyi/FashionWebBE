using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Repositories
{
    public class RedisCache
    {
        private readonly IDatabase _db;

        public RedisCache(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<T?> GetDataAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue) return default;
            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetDataAsync<T>(string key, T data, TimeSpan? absoluteExpireTime = null)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var serializedData = JsonSerializer.Serialize(data, options);
            await _db.StringSetAsync(key, serializedData, absoluteExpireTime ?? TimeSpan.FromDays(7));
        }

        public async Task RemoveDataAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? absoluteExpireTime = null)
        {
            var cachedValue = await GetDataAsync<T>(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var dbValue = await factory();
            if (dbValue != null)
            {
                await SetDataAsync(key, dbValue, absoluteExpireTime);
            }

            return dbValue;
        }
    }
}
