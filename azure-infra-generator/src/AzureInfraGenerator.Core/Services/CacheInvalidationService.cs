using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Services
{
    /// <summary>
    /// Manages cache invalidation strategies
    /// </summary>
    public interface ICacheInvalidationService
    {
        /// <summary>
        /// Invalidate cache for a specific key
        /// </summary>
        Task InvalidateCacheAsync(string key, ScriptType scriptType);

        /// <summary>
        /// Invalidate all caches for a specific script type
        /// </summary>
        Task InvalidateByScriptTypeAsync(ScriptType scriptType);

        /// <summary>
        /// Invalidate cache based on age
        /// </summary>
        Task InvalidateByAgeAsync(TimeSpan maxAge);

        /// <summary>
        /// Invalidate cache based on custom predicate
        /// </summary>
        Task InvalidateByPredicateAsync(Func<CacheMetadata, bool> predicate);
    }

    /// <summary>
    /// Comprehensive cache invalidation service
    /// </summary>
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly ICacheStrategy _cacheStrategy;
        private readonly ILogger<CacheInvalidationService> _logger;
        private readonly ConcurrentDictionary<string, CacheMetadata> _cacheRegistry;

        public CacheInvalidationService(
            ICacheStrategy cacheStrategy, 
            ILogger<CacheInvalidationService> logger)
        {
            _cacheStrategy = cacheStrategy ?? 
                throw new ArgumentNullException(nameof(cacheStrategy));
            
            _logger = logger ?? 
                throw new ArgumentNullException(nameof(logger));
            
            _cacheRegistry = new ConcurrentDictionary<string, CacheMetadata>();
        }

        /// <summary>
        /// Track cache entry for management
        /// </summary>
        public void TrackCacheEntry(string key, CacheMetadata metadata)
        {
            _cacheRegistry[key] = metadata;
        }

        /// <summary>
        /// Invalidate a specific cache key
        /// </summary>
        public async Task InvalidateCacheAsync(string key, ScriptType scriptType)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty", nameof(key));

            try
            {
                await _cacheStrategy.RemoveAsync(key);
                
                // Remove from registry
                _cacheRegistry.TryRemove(key, out _);

                _logger.LogInformation($"Invalidated cache for key: {key}, ScriptType: {scriptType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to invalidate cache for key: {key}");
                throw;
            }
        }

        /// <summary>
        /// Invalidate all caches for a specific script type
        /// </summary>
        public async Task InvalidateByScriptTypeAsync(ScriptType scriptType)
        {
            var keysToInvalidate = _cacheRegistry
                .Where(entry => entry.Value.ScriptType == scriptType)
                .Select(entry => entry.Key)
                .ToList();

            foreach (var key in keysToInvalidate)
            {
                await InvalidateCacheAsync(key, scriptType);
            }

            _logger.LogInformation($"Invalidated all caches for ScriptType: {scriptType}");
        }

        /// <summary>
        /// Invalidate cache entries older than specified age
        /// </summary>
        public async Task InvalidateByAgeAsync(TimeSpan maxAge)
        {
            var now = DateTime.UtcNow;
            var keysToInvalidate = _cacheRegistry
                .Where(entry => now - entry.Value.FirstCacheTime > maxAge)
                .Select(entry => entry.Key)
                .ToList();

            foreach (var key in keysToInvalidate)
            {
                await InvalidateCacheAsync(key, _cacheRegistry[key].ScriptType);
            }

            _logger.LogInformation($"Invalidated caches older than {maxAge}");
        }

        /// <summary>
        /// Invalidate cache based on custom predicate
        /// </summary>
        public async Task InvalidateByPredicateAsync(Func<CacheMetadata, bool> predicate)
        {
            var keysToInvalidate = _cacheRegistry
                .Where(entry => predicate(entry.Value))
                .Select(entry => entry.Key)
                .ToList();

            foreach (var key in keysToInvalidate)
            {
                await InvalidateCacheAsync(key, _cacheRegistry[key].ScriptType);
            }

            _logger.LogInformation("Invalidated caches based on custom predicate");
        }

        /// <summary>
        /// Periodic cache cleanup
        /// </summary>
        public async Task PerformPeriodicCleanupAsync()
        {
            // Remove expired entries
            await InvalidateByAgeAsync(TimeSpan.FromDays(7));

            // Remove least used entries if cache grows too large
            if (_cacheRegistry.Count > 1000)
            {
                var leastUsedKeys = _cacheRegistry
                    .OrderBy(entry => entry.Value.HitCount)
                    .Take(200)
                    .Select(entry => entry.Key)
                    .ToList();

                foreach (var key in leastUsedKeys)
                {
                    await InvalidateCacheAsync(key, _cacheRegistry[key].ScriptType);
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for cache invalidation
    /// </summary>
    public static class CacheInvalidationExtensions
    {
        /// <summary>
        /// Add cache invalidation services
        /// </summary>
        public static IServiceCollection AddCacheInvalidation(
            this IServiceCollection services)
        {
            services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();
            
            // Optional: Add background service for periodic cleanup
            services.AddHostedService<CacheCleanupBackgroundService>();

            return services;
        }
    }

    /// <summary>
    /// Background service for periodic cache cleanup
    /// </summary>
    public class CacheCleanupBackgroundService : BackgroundService
    {
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ILogger<CacheCleanupBackgroundService> _logger;

        public CacheCleanupBackgroundService(
            ICacheInvalidationService cacheInvalidationService,
            ILogger<CacheCleanupBackgroundService> logger)
        {
            _cacheInvalidationService = cacheInvalidationService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _cacheInvalidationService.PerformPeriodicCleanupAsync();
                    
                    // Run cleanup every 24 hours
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cache cleanup");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }
    }
}
