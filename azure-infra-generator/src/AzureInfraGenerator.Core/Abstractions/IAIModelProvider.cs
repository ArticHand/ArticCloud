using System.Threading.Tasks;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Defines a contract for AI model providers
    /// </summary>
    public interface IAIModelProvider
    {
        /// <summary>
        /// Gets the supported AI model
        /// </summary>
        AIModel SupportedModel { get; }

        /// <summary>
        /// Generates a text completion based on system and user prompts
        /// </summary>
        /// <param name="systemPrompt">System context prompt</param>
        /// <param name="userPrompt">User-specific prompt</param>
        /// <param name="maxTokens">Maximum number of tokens to generate</param>
        /// <returns>Generated text completion</returns>
        Task<string> GenerateCompletionAsync(
            string systemPrompt, 
            string userPrompt, 
            int maxTokens = 2000);
    }
}
