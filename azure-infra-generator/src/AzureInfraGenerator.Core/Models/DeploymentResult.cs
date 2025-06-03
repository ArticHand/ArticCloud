using System;
using System.Collections.Generic;

namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Result of an Azure deployment operation
    /// </summary>
    public class DeploymentResult
    {
        /// <summary>
        /// Unique identifier for the deployment
        /// </summary>
        public string DeploymentId { get; set; }

        /// <summary>
        /// Name of the deployment
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// Current status of the deployment
        /// </summary>
        public DeploymentStatus Status { get; set; }

        /// <summary>
        /// Timestamp when the deployment started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Timestamp when the deployment completed (if applicable)
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Target resource group for the deployment
        /// </summary>
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Azure region for the deployment
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Type of script used for deployment
        /// </summary>
        public ScriptType ScriptType { get; set; }

        /// <summary>
        /// List of resources created or modified by this deployment
        /// </summary>
        public List<DeployedResource> Resources { get; set; } = new List<DeployedResource>();

        /// <summary>
        /// Error message if deployment failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Detailed error information if available
        /// </summary>
        public string DetailedError { get; set; }

        /// <summary>
        /// URL to view the deployment in Azure Portal (if available)
        /// </summary>
        public string PortalUrl { get; set; }

        /// <summary>
        /// Duration of the deployment
        /// </summary>
        public TimeSpan Duration => EndTime.HasValue 
            ? EndTime.Value - StartTime 
            : DateTime.UtcNow - StartTime;

        /// <summary>
        /// Whether the deployment was successful
        /// </summary>
        public bool IsSuccessful => Status == DeploymentStatus.Succeeded;
    }
}
