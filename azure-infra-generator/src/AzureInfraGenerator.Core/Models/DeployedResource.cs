using System;

namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Represents an Azure resource created or modified during deployment
    /// </summary>
    public class DeployedResource
    {
        /// <summary>
        /// Resource ID in Azure
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Name of the resource
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the Azure resource
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Provisioning state of the resource
        /// </summary>
        public string ProvisioningState { get; set; }

        /// <summary>
        /// Timestamp when the resource was created or last modified
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether the resource was newly created
        /// </summary>
        public bool IsNewResource { get; set; }

        /// <summary>
        /// URL to view the resource in Azure Portal
        /// </summary>
        public string PortalUrl { get; set; }
    }
}
