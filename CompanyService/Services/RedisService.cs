using StackExchange.Redis;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using CompanyService.Models;
using Microsoft.Extensions.Options;

namespace CompanyService.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _database;
        private readonly TimeSpan _defaultTTL;
            
        public RedisService(IConnectionMultiplexer multiplexer, IOptions<CacheSettings> options)
        {
            _database = multiplexer.GetDatabase();
            _defaultTTL = TimeSpan.FromMinutes(options.Value.DefaultTTLMinutes);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"[Error] Failed to connect to Redis: {ex.Message}");
                return default; 
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _database.StringSetAsync(key, json, ttl ?? _defaultTTL);
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"[Error] Failed to connect to Redis on SET: {ex.Message}");
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"[Error] Failed to connect to Redis on DELETE: {ex.Message}");
            }
        }
    }
}
