using System.Collections.Generic;
using System.Threading.Tasks;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Interface for verifying Azure resources after deployment
    /// </summary>
    public interface IResourceVerificationService
    {
        /// <summary>
        /// Verifies the health and status of deployed resources
        /// </summary>
        /// <param name="deploymentResult">The deployment result containing resources to verify</param>
        /// <returns>Verification result with status and details</returns>
        Task<ResourceVerificationResult> VerifyResourcesAsync(DeploymentResult deploymentResult);

        /// <summary>
        /// Verifies a specific resource by its ID
        /// </summary>
        /// <param name="resourceId">The Azure resource ID to verify</param>
        /// <returns>Verification result for the specific resource</returns>
        Task<ResourceVerificationItem> VerifyResourceAsync(string resourceId);

        /// <summary>
        /// Monitors resources for a specified period to ensure stability
        /// </summary>
        /// <param name="deploymentResult">The deployment result containing resources to monitor</param>
        /// <param name="monitoringPeriodMinutes">How long to monitor resources in minutes</param>
        /// <returns>Monitoring result with status and metrics</returns>
        Task<ResourceMonitoringResult> MonitorResourcesAsync(DeploymentResult deploymentResult, int monitoringPeriodMinutes = 5);
    }
}
