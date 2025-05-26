using System;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Configuration
{
    /// <summary>
    /// Configuration options for infrastructure script generation
    /// </summary>
    public class InfrastructureGeneratorOptions
    {
        /// <summary>
        /// Preferred AI model for script generation
        /// </summary>
        public AIModel PreferredModel { get; set; } = AIModel.GPT_4;

        /// <summary>
        /// Maximum number of tokens for script generation
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// Enable or disable script caching
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Logging level for generation process
        /// </summary>
        public LogLevel LoggingLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Maximum cache size (number of entries)
        /// </summary>
        public int MaxCacheSize { get; set; } = 100;

        /// <summary>
        /// Cache expiration time
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Enable detailed telemetry tracking
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;

        /// <summary>
        /// Validate configuration settings
        /// </summary>
        public void Validate()
        {
            if (MaxTokens <= 0)
                throw new ArgumentException("Max tokens must be positive", nameof(MaxTokens));

            if (MaxCacheSize < 0)
                throw new ArgumentException("Max cache size cannot be negative", nameof(MaxCacheSize));
        }
    }
}
