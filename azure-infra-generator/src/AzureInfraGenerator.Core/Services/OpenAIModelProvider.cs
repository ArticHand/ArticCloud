using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Models;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace AzureInfraGenerator.Core.Services
{
    /// <summary>
    /// OpenAI implementation of AI model provider
    /// </summary>
    public class OpenAIModelProvider : IAIModelProvider
    {
        private readonly IOpenAIService _openAiService;
        private readonly ILogger<OpenAIModelProvider> _logger;
        private readonly AIModel _supportedModel;

        /// <summary>
        /// Supported AI model
        /// </summary>
        public AIModel SupportedModel => _supportedModel;

        /// <summary>
        /// Initializes a new instance of OpenAI Model Provider
        /// </summary>
        /// <param name="configuration">Configuration provider</param>
        /// <param name="logger">Logging service</param>
        public OpenAIModelProvider(
            IConfiguration configuration, 
            ILogger<OpenAIModelProvider> logger)
        {
            var apiKey = configuration["OpenAI:ApiKey"] ?? 
                throw new InvalidOperationException("OpenAI API Key is missing");

            _logger = logger ?? 
                throw new ArgumentNullException(nameof(logger));

            // Determine model from configuration
            var modelStr = configuration["InfrastructureGenerator:PreferredModel"] ?? "GPT_4";
            _supportedModel = Enum.TryParse(modelStr, out AIModel model) 
                ? model 
                : AIModel.GPT_4;

            _openAiService = new OpenAIService(new OpenAiOptions
            {
                ApiKey = apiKey
            });

            _logger.LogInformation($"Initialized OpenAI Model Provider with {_supportedModel}");
        }

        /// <summary>
        /// Generates AI completion for infrastructure script
        /// </summary>
        public async Task<string> GenerateCompletionAsync(
            string systemPrompt, 
            string userPrompt, 
            int maxTokens)
        {
            try 
            {
                var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem(systemPrompt),
                        ChatMessage.FromUser(userPrompt)
                    },
                    Model = GetOpenAIModelString(_supportedModel),
                    MaxTokens = maxTokens
                });

                if (completionResult.Successful)
                {
                    var generatedScript = completionResult.Choices[0].Message.Content;
                    _logger.LogInformation($"Successfully generated script using {_supportedModel}");
                    return generatedScript;
                }
                else
                {
                    _logger.LogError($"OpenAI API error: {completionResult.Error?.Message}");
                    throw new InvalidOperationException(
                        $"Script generation failed: {completionResult.Error?.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI completion");
                throw;
            }
        }

        /// <summary>
        /// Maps AIModel enum to OpenAI model string
        /// </summary>
        private string GetOpenAIModelString(AIModel model) => model switch
        {
            AIModel.GPT_3_5_Turbo => OpenAI.ObjectModels.Models.Gpt_3_5_Turbo,
            AIModel.GPT_4 => OpenAI.ObjectModels.Models.Gpt_4,
            AIModel.GPT_4_Turbo => OpenAI.ObjectModels.Models.Gpt_4_Turbo,
            _ => OpenAI.ObjectModels.Models.Gpt_4
        };
    }
}