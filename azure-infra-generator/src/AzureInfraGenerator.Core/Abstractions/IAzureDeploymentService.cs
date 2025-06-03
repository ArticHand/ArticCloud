using System;
using System.Threading.Tasks;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Interface for Azure deployment service to deploy generated infrastructure scripts
    /// </summary>
    public interface IAzureDeploymentService
    {
        /// <summary>
        /// Deploys an infrastructure script to Azure
        /// </summary>
        /// <param name="script">The infrastructure script to deploy</param>
        /// <param name="scriptType">Type of the script (Bicep, Terraform, etc.)</param>
        /// <param name="deploymentName">Optional name for the deployment</param>
        /// <param name="resourceGroupName">Target resource group name</param>
        /// <param name="location">Azure region for deployment</param>
        /// <returns>Deployment result with status and details</returns>
        Task<DeploymentResult> DeployAsync(
            string script, 
            ScriptType scriptType, 
            string deploymentName = null, 
            string resourceGroupName = null, 
            string location = null);

        /// <summary>
        /// Gets the status of an ongoing or completed deployment
        /// </summary>
        /// <param name="deploymentId">The ID of the deployment to check</param>
        /// <returns>Current deployment status</returns>
        Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId);

        /// <summary>
        /// Cancels an ongoing deployment
        /// </summary>
        /// <param name="deploymentId">The ID of the deployment to cancel</param>
        /// <returns>True if cancellation was successful</returns>
        Task<bool> CancelDeploymentAsync(string deploymentId);
    }
}
