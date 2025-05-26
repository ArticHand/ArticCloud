using System;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Represents detailed metrics for infrastructure script generation
    /// </summary>
    public class ScriptGenerationMetrics
    {
        private readonly ICostCalculator _costCalculator;

        /// <summary>
        /// Time taken to generate the script
        /// </summary>
        public TimeSpan GenerationTime { get; set; }

        /// <summary>
        /// Number of tokens used in generation
        /// </summary>
        public int TokensUsed { get; set; }

        /// <summary>
        /// AI Model used for generation
        /// </summary>
        public AIModel ModelUsed { get; set; }

        /// <summary>
        /// Type of script generated
        /// </summary>
        public ScriptType ScriptType { get; set; }

        /// <summary>
        /// Timestamp of script generation
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates if the script was retrieved from cache
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>
        /// Total cost of API call
        /// </summary>
        public decimal EstimatedCost { get; private set; }

        /// <summary>
        /// Detailed cache information
        /// </summary>
        public CacheMetadata CacheDetails { get; set; }

        /// <summary>
        /// Constructor with dependency injection for cost calculation
        /// </summary>
        public ScriptGenerationMetrics(ICostCalculator costCalculator = null)
        {
            _costCalculator = costCalculator ?? new OpenAICostCalculator();
        }

        /// <summary>
        /// Calculates and sets the estimated cost
        /// </summary>
        public void CalculateCost()
        {
            EstimatedCost = _costCalculator.CalculateCost(ModelUsed, TokensUsed);
        }
    }

    /// <summary>
    /// Represents detailed cache metadata
    /// </summary>
    public class CacheMetadata
    {
        /// <summary>
        /// Unique cache key
        /// </summary>
        public string CacheKey { get; set; }

        /// <summary>
        /// Cache hit count
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// Timestamp of first cache entry
        /// </summary>
        public DateTime FirstCacheTime { get; set; }

        /// <summary>
        /// Timestamp of last cache access
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// Cache entry size in bytes
        /// </summary>
        public long CacheSize { get; set; }

        /// <summary>
        /// Indicates cache entry expiration status
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// Cache strategy used
        /// </summary>
        public CacheStrategy Strategy { get; set; }
    }

    /// <summary>
    /// Defines different caching strategies
    /// </summary>
    public enum CacheStrategy
    {
        /// <summary>
        /// No caching
        /// </summary>
        None,

        /// <summary>
        /// Least Recently Used (LRU) caching
        /// </summary>
        LeastRecentlyUsed,

        /// <summary>
        /// First In First Out (FIFO) caching
        /// </summary>
        FirstInFirstOut,

        /// <summary>
        /// Least Frequently Used (LFU) caching
        /// </summary>
        LeastFrequentlyUsed,

        /// <summary>
        /// Time-based expiration caching
        /// </summary>
        TimeBasedExpiration,

        /// <summary>
        /// Distributed Redis caching
        /// </summary>
        DistributedRedis
    }
        /// </summary>
        public ScriptType ScriptType { get; set; }

        /// <summary>
        /// Timestamp of script generation
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates if the script was retrieved from cache
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>
        /// Total cost of API call
        /// </summary>
        public decimal EstimatedCost { get; set; }
    }
}
