using System.Threading.Tasks;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Defines the contract for AI model providers
    /// </summary>
    public interface IModelProvider
    {
        /// <summary>
        /// Generates text completion based on given prompt
        /// </summary>
        /// <param name="systemPrompt">System instruction for the model</param>
        /// <param name="userPrompt">User's specific prompt</param>
        /// <returns>Generated text completion</returns>
        Task<string> GenerateCompletionAsync(string systemPrompt, string userPrompt);
    }
}
