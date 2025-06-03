using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Configuration;
using AzureInfraGenerator.Core.Exceptions;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Services
{
    /// <summary>
    /// Infrastructure script generator with advanced features
    /// </summary>
    public class InfrastructureGenerator : IInfrastructureGenerator
    {
        private readonly IAIModelProvider _modelProvider;
        private readonly IScriptCache _scriptCache;
        private readonly IScriptValidator _scriptValidator;
        private readonly ILogger<OpenAIInfrastructureGenerator> _logger;
        private readonly InfrastructureGeneratorOptions _options;

        /// <summary>
        /// Initializes a new instance of the Infrastructure Generator
        /// </summary>
        /// <param name="modelProvider">AI model provider</param>
        /// <param name="scriptCache">Script caching mechanism</param>
        /// <param name="scriptValidator">Script validation service</param>
        /// <param name="logger">Logging service</param>
        /// <param name="options">Configuration options</param>
        public OpenAIInfrastructureGenerator(
            IAIModelProvider modelProvider,
            IScriptCache scriptCache,
            IScriptValidator scriptValidator,
            ILogger<OpenAIInfrastructureGenerator> logger,
            InfrastructureGeneratorOptions options = null)
        {
            _modelProvider = modelProvider ?? 
                throw new ArgumentNullException(nameof(modelProvider));
            
            _scriptCache = scriptCache ?? 
                throw new ArgumentNullException(nameof(scriptCache));
            
            _scriptValidator = scriptValidator ?? 
                throw new ArgumentNullException(nameof(scriptValidator));
            
            _logger = logger ?? 
                throw new ArgumentNullException(nameof(logger));
            
            _options = options ?? new InfrastructureGeneratorOptions();
            _options.Validate();
        }

        /// <summary>
        /// Generates an infrastructure script with advanced features
        /// </summary>
        public async Task<InfrastructureScriptResult> GenerateScriptAsync(
            string userPrompt, 
            ScriptType scriptType = ScriptType.Bicep)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(userPrompt))
                throw new ArgumentException("User prompt cannot be empty.", nameof(userPrompt));

            // Performance tracking
            var startTime = DateTime.UtcNow;

            // Check cache first
            if (_options.EnableCaching)
            {
                var cachedScript = await _scriptCache.GetCachedScriptAsync(userPrompt, scriptType);
                if (!string.IsNullOrEmpty(cachedScript))
                {
                    _logger.LogInformation("Script retrieved from cache");
                    return CreateScriptResult(cachedScript, scriptType, startTime, true);
                }
            }

            // Customize system prompt based on script type
            var systemPrompt = GetSystemPromptForScriptType(scriptType);

            try
            {
                // Generate script
                var generatedScript = await _modelProvider.GenerateCompletionAsync(
                    systemPrompt, 
                    userPrompt, 
                    _options.MaxTokens);

                // Validate script
                var validationResult = await _scriptValidator.ValidateScriptAsync(generatedScript, scriptType);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Script validation found issues");
                    foreach (var issue in validationResult.Issues)
                    {
                        _logger.LogWarning($"Issue: {issue.Description} (Severity: {issue.Severity})");
                    }
                }

                // Cache script if enabled
                if (_options.EnableCaching)
                {
                    await _scriptCache.CacheScriptAsync(userPrompt, scriptType, generatedScript);
                }

                return CreateScriptResult(generatedScript, scriptType, startTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Script generation failed");
                throw new InfrastructureGenerationException(
                    $"Script generation failed: {ex.Message}", 
                    scriptType, 
                    ex);
            }
        }

        private InfrastructureScriptResult CreateScriptResult(
            string script, 
            ScriptType scriptType, 
            DateTime startTime, 
            bool isCached = false)
        {
            return new InfrastructureScriptResult
            {
                Script = script,
                ScriptType = scriptType,
                GeneratedAt = DateTime.UtcNow,
                ModelUsed = _modelProvider.SupportedModel.ToString(),
                Metrics = new ScriptGenerationMetrics
                {
                    GenerationTime = DateTime.UtcNow - startTime,
                    ModelUsed = _modelProvider.SupportedModel,
                    ScriptType = scriptType,
                    IsCached = isCached
                }
            };
        }

        /// <summary>
        /// Gets the appropriate system prompt for a given script type
        /// </summary>
        private string GetSystemPromptForScriptType(ScriptType scriptType)
        {
            return scriptType switch
            {
                ScriptType.Bicep => 
                    "You are an expert Azure Bicep script generator. Generate a precise, production-ready Bicep script for the given cloud deployment needs. " +
                    "Ensure the script follows best practices, includes necessary parameters, and is well-commented. " +
                    "Provide a modular and reusable infrastructure-as-code solution.",

                ScriptType.Terraform => 
                    "You are an expert Terraform script generator for Azure. Create a comprehensive Terraform script that meets the specified cloud deployment requirements. " +
                    "Include proper resource configurations, variables, and follow Terraform best practices. " +
                    "Ensure the script is modular, uses appropriate provider configurations, and includes necessary outputs.",

                ScriptType.PowerShell => 
                    "You are an expert Azure PowerShell deployment script generator. Produce a robust PowerShell script for the described cloud infrastructure. " +
                    "Include error handling, logging, and follow Azure PowerShell module best practices. " +
                    "Provide a script that is idempotent, handles potential failures, and includes comprehensive parameter validation.",

                _ => throw new ArgumentOutOfRangeException(nameof(scriptType), "Unsupported script type")
            };
        }
    }
}