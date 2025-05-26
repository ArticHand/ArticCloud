using System.Threading.Tasks;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Defines the contract for generating specific script types
    /// </summary>
    public interface IScriptGenerator
    {
        /// <summary>
        /// Generates a script for a specific infrastructure type
        /// </summary>
        /// <param name="prompt">User's deployment description</param>
        /// <returns>Generated script content</returns>
        Task<string> GenerateScriptAsync(string prompt);

        /// <summary>
        /// Gets the script type this generator supports
        /// </summary>
        ScriptType SupportedScriptType { get; }
    }
}
