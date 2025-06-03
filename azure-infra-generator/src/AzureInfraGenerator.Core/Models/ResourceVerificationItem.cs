using System;
using System.Collections.Generic;

namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Verification result for a specific Azure resource
    /// </summary>
    public class ResourceVerificationItem
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
        /// Verification status for this resource
        /// </summary>
        public VerificationStatus Status { get; set; }

        /// <summary>
        /// Current provisioning state of the resource
        /// </summary>
        public string ProvisioningState { get; set; }

        /// <summary>
        /// When the verification was performed
        /// </summary>
        public DateTime VerificationTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Issues found during verification
        /// </summary>
        public List<VerificationIssue> Issues { get; set; } = new List<VerificationIssue>();

        /// <summary>
        /// Health metrics for the resource (if available)
        /// </summary>
        public Dictionary<string, string> Metrics { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Whether the resource is accessible
        /// </summary>
        public bool IsAccessible { get; set; }

        /// <summary>
        /// Whether the resource is properly configured
        /// </summary>
        public bool IsProperlyConfigured { get; set; }

        /// <summary>
        /// Whether the resource has any security issues
        /// </summary>
        public bool HasSecurityIssues { get; set; }

        /// <summary>
        /// URL to view the resource in Azure Portal
        /// </summary>
        public string PortalUrl { get; set; }
    }
}
