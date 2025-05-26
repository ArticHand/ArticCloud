using System;
using System.Threading.Tasks;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Defines a contract for cache management strategies
    /// </summary>
    public interface ICacheStrategy
    {
        /// <summary>
        /// Unique identifier for the cache strategy
        /// </summary>
        CacheStrategy StrategyType { get; }

        /// <summary>
        /// Adds an item to the cache
        /// </summary>
        Task CacheItemAsync(string key, object value);

        /// <summary>
        /// Retrieves an item from the cache
        /// </summary>
        Task<object> GetCachedItemAsync(string key);

        /// <summary>
        /// Checks if an item exists in the cache
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Clears all cached items
        /// </summary>
        Task ClearAsync();
    }

    /// <summary>
    /// Least Recently Used (LRU) Cache Strategy
    /// </summary>
    public class LeastRecentlyUsedCacheStrategy : ICacheStrategy
    {
        private readonly int _maxSize;
        private readonly Dictionary<string, (object Value, DateTime LastAccessed)> _cache;

        public CacheStrategy StrategyType => CacheStrategy.LeastRecentlyUsed;

        public LeastRecentlyUsedCacheStrategy(int maxSize = 100)
        {
            _maxSize = maxSize;
            _cache = new Dictionary<string, (object, DateTime)>();
        }

        public Task CacheItemAsync(string key, object value)
        {
            if (_cache.Count >= _maxSize)
            {
                // Remove least recently used item
                var oldestKey = _cache
                    .OrderBy(x => x.Value.LastAccessed)
                    .First().Key;
                _cache.Remove(oldestKey);
            }

            _cache[key] = (value, DateTime.UtcNow);
            return Task.CompletedTask;
        }

        public Task<object> GetCachedItemAsync(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                // Update last accessed time
                _cache[key] = (item.Value, DateTime.UtcNow);
                return Task.FromResult(item.Value);
            }
            return Task.FromResult<object>(null);
        }

        public Task<bool> ExistsAsync(string key) => 
            Task.FromResult(_cache.ContainsKey(key));

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _cache.Clear();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Time-Based Expiration Cache Strategy
    /// </summary>
    public class TimeBasedExpirationCacheStrategy : ICacheStrategy
    {
        private readonly TimeSpan _expirationTime;
        private readonly Dictionary<string, (object Value, DateTime CreatedAt)> _cache;

        public CacheStrategy StrategyType => CacheStrategy.TimeBasedExpiration;

        public TimeBasedExpirationCacheStrategy(TimeSpan? expirationTime = null)
        {
            _expirationTime = expirationTime ?? TimeSpan.FromHours(1);
            _cache = new Dictionary<string, (object, DateTime)>();
        }

        public Task CacheItemAsync(string key, object value)
        {
            // Remove expired items
            RemoveExpiredItems();

            _cache[key] = (value, DateTime.UtcNow);
            return Task.CompletedTask;
        }

        public Task<object> GetCachedItemAsync(string key)
        {
            RemoveExpiredItems();

            if (_cache.TryGetValue(key, out var item) && 
                DateTime.UtcNow - item.CreatedAt <= _expirationTime)
            {
                return Task.FromResult(item.Value);
            }
            return Task.FromResult<object>(null);
        }

        private void RemoveExpiredItems()
        {
            var expiredKeys = _cache
                .Where(x => DateTime.UtcNow - x.Value.CreatedAt > _expirationTime)
                .Select(x => x.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }
        }

        public Task<bool> ExistsAsync(string key)
        {
            RemoveExpiredItems();
            return Task.FromResult(_cache.ContainsKey(key));
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _cache.Clear();
            return Task.CompletedTask;
        }
    }
}
