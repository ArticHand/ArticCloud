using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Distributed Redis cache strategy
    /// </summary>
    public class RedisCacheStrategy : ICacheStrategy
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ConnectionMultiplexer _redisConnection;
        private readonly IDatabase _redisDatabase;

        public CacheStrategy StrategyType => CacheStrategy.DistributedRedis;

        /// <summary>
        /// Constructor for Redis cache strategy
        /// </summary>
        /// <param name="connectionString">Redis connection string</param>
        public RedisCacheStrategy(string connectionString)
        {
            // Validate connection string
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Redis connection string cannot be empty", nameof(connectionString));

            // Configure Redis connection
            _redisConnection = ConnectionMultiplexer.Connect(connectionString);
            _redisDatabase = _redisConnection.GetDatabase();
        }

        /// <summary>
        /// Cache an item in Redis
        /// </summary>
        public async Task CacheItemAsync(string key, object value)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty", nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot cache null value");

            // Serialize the value
            var serializedValue = JsonSerializer.Serialize(value);
            
            // Set cache with default expiration of 1 hour
            await _redisDatabase.StringSetAsync(
                key, 
                serializedValue, 
                TimeSpan.FromHours(1)
            );
        }

        /// <summary>
        /// Retrieve a cached item from Redis
        /// </summary>
        public async Task<object> GetCachedItemAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty", nameof(key));

            var cachedValue = await _redisDatabase.StringGetAsync(key);
            
            if (!cachedValue.HasValue)
                return null;

            // Deserialize the value
            return JsonSerializer.Deserialize<object>(cachedValue);
        }

        /// <summary>
        /// Check if an item exists in the cache
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            return await _redisDatabase.KeyExistsAsync(key);
        }

        /// <summary>
        /// Remove an item from the cache
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            await _redisDatabase.KeyDeleteAsync(key);
        }

        /// <summary>
        /// Clear all items from the cache
        /// </summary>
        public async Task ClearAsync()
        {
            // Flush entire database
            var endpoints = _redisConnection.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redisConnection.GetServer(endpoint);
                await server.FlushAllDatabasesAsync();
            }
        }

        /// <summary>
        /// Dispose Redis connection
        /// </summary>
        public void Dispose()
        {
            _redisConnection?.Close();
            _redisConnection?.Dispose();
        }
    }

    /// <summary>
    /// Extension method to add Redis caching
    /// </summary>
    public static class RedisCacheExtensions
    {
        /// <summary>
        /// Add distributed Redis caching to service collection
        /// </summary>
        public static IServiceCollection AddRedisCache(
            this IServiceCollection services, 
            string connectionString)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
            });

            services.AddSingleton<ICacheStrategy>(
                new RedisCacheStrategy(connectionString));

            return services;
        }
    }
}
