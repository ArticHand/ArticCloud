using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Services
{
    /// <summary>
    /// Factory for creating AI model providers
    /// </summary>
    public class AIModelProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIModelProviderFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the AI Model Provider Factory
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">Logger</param>
        public AIModelProviderFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AIModelProviderFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates an AI model provider based on configuration
        /// </summary>
        /// <returns>AI model provider</returns>
        public IAIModelProvider CreateProvider()
        {
            // Get provider type from configuration
            var providerTypeStr = _configuration["InfrastructureGenerator:AIProvider"] ?? "OpenAI";
            
            if (!Enum.TryParse<AIProviderType>(providerTypeStr, true, out var providerType))
            {
                _logger.LogWarning($"Unknown AI provider type: {providerTypeStr}. Defaulting to OpenAI.");
                providerType = AIProviderType.OpenAI;
            }

            // Get model from configuration
            var modelStr = _configuration["InfrastructureGenerator:PreferredModel"] ?? "GPT_4";
            
            if (!Enum.TryParse<AIModel>(modelStr, out var model))
            {
                _logger.LogWarning($"Unknown model: {modelStr}. Defaulting to GPT_4.");
                model = AIModel.GPT_4;
            }

            // Create appropriate provider based on model and provider type
            switch (providerType)
            {
                case AIProviderType.Claude:
                    _logger.LogInformation("Creating Claude model provider");
                    return ActivatorUtilities.CreateInstance<ClaudeModelProvider>(_serviceProvider);
                
                case AIProviderType.OpenAI:
                default:
                    _logger.LogInformation("Creating OpenAI model provider");
                    return ActivatorUtilities.CreateInstance<OpenAIModelProvider>(_serviceProvider);
            }
        }

        /// <summary>
        /// Determines the appropriate provider type for a given model
        /// </summary>
        /// <param name="model">AI model</param>
        /// <returns>Provider type</returns>
        public static AIProviderType GetProviderTypeForModel(AIModel model)
        {
            return model switch
            {
                AIModel.Claude_3_Opus => AIProviderType.Claude,
                AIModel.Claude_3_Sonnet => AIProviderType.Claude,
                AIModel.Claude_3_Haiku => AIProviderType.Claude,
                _ => AIProviderType.OpenAI
            };
        }
    }
}
