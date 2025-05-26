using System.Threading.Tasks;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Defines the contract for generating infrastructure scripts
    /// </summary>
    public interface IInfrastructureGenerator
    {
        /// <summary>
        /// Generates an infrastructure deployment script
        /// </summary>
        /// <param name="userPrompt">User's cloud deployment description</param>
        /// <param name="scriptType">Desired script type</param>
        /// <returns>Infrastructure script generation result</returns>
        Task<InfrastructureScriptResult> GenerateScriptAsync(
            string userPrompt, 
            ScriptType scriptType = ScriptType.Bicep);
    }
}
