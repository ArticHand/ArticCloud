using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Services
{
    /// <summary>
    /// Anthropic Claude implementation of AI model provider
    /// </summary>
    public class ClaudeModelProvider : IAIModelProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClaudeModelProvider> _logger;
        private readonly AIModel _supportedModel;
        private readonly string _apiKey;
        private readonly string _apiVersion;
        private const string BaseUrl = "https://api.anthropic.com";

        /// <summary>
        /// Supported AI model
        /// </summary>
        public AIModel SupportedModel => _supportedModel;

        /// <summary>
        /// Initializes a new instance of Claude Model Provider
        /// </summary>
        /// <param name="configuration">Configuration provider</param>
        /// <param name="logger">Logging service</param>
        public ClaudeModelProvider(
            IConfiguration configuration, 
            ILogger<ClaudeModelProvider> logger)
        {
            _apiKey = configuration["Claude:ApiKey"] ?? 
                throw new InvalidOperationException("Claude API Key is missing");
            
            _apiVersion = configuration["Claude:ApiVersion"] ?? "2023-06-01";

            _logger = logger ?? 
                throw new ArgumentNullException(nameof(logger));

            // Determine model from configuration
            var modelStr = configuration["InfrastructureGenerator:PreferredModel"] ?? "Claude_3_Sonnet";
            _supportedModel = Enum.TryParse(modelStr, out AIModel model) && IsClaudeModel(model)
                ? model 
                : AIModel.Claude_3_Sonnet;

            // Initialize HttpClient
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", _apiVersion);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation($"Initialized Claude Model Provider with {_supportedModel}");
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
                var requestBody = new
                {
                    model = GetClaudeModelString(_supportedModel),
                    max_tokens = maxTokens,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}/v1/messages", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<ClaudeCompletionResponse>(responseBody);

                if (responseObject?.Content?.Count > 0)
                {
                    var generatedScript = responseObject.Content[0].Text;
                    _logger.LogInformation($"Successfully generated script using {_supportedModel}");
                    return generatedScript;
                }
                else
                {
                    _logger.LogError("Claude API returned empty response");
                    throw new InvalidOperationException("Script generation failed: Empty response from Claude API");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI completion with Claude");
                throw;
            }
        }

        /// <summary>
        /// Maps AIModel enum to Claude model string
        /// </summary>
        private string GetClaudeModelString(AIModel model) => model switch
        {
            AIModel.Claude_3_Opus => "claude-3-opus-20240229",
            AIModel.Claude_3_Sonnet => "claude-3-sonnet-20240229",
            AIModel.Claude_3_Haiku => "claude-3-haiku-20240307",
            _ => "claude-3-sonnet-20240229" // Default to Sonnet if not specified
        };

        /// <summary>
        /// Checks if the model is a Claude model
        /// </summary>
        private bool IsClaudeModel(AIModel model) => model switch
        {
            AIModel.Claude_3_Opus => true,
            AIModel.Claude_3_Sonnet => true,
            AIModel.Claude_3_Haiku => true,
            _ => false
        };
    }

    /// <summary>
    /// Response structure for Claude API
    /// </summary>
    internal class ClaudeCompletionResponse
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Model { get; set; }
        public List<ClaudeContent> Content { get; set; }
    }

    /// <summary>
    /// Content structure for Claude API response
    /// </summary>
    internal class ClaudeContent
    {
        public string Type { get; set; }
        public string Text { get; set; }
    }
}
