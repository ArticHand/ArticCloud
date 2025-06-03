using System;
using System.Collections.Generic;

namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Result of resource verification process
    /// </summary>
    public class ResourceVerificationResult
    {
        /// <summary>
        /// ID of the deployment that was verified
        /// </summary>
        public string DeploymentId { get; set; }

        /// <summary>
        /// When the verification was performed
        /// </summary>
        public DateTime VerificationTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Overall verification status
        /// </summary>
        public VerificationStatus Status { get; set; }

        /// <summary>
        /// Detailed results for each verified resource
        /// </summary>
        public List<ResourceVerificationItem> Resources { get; set; } = new List<ResourceVerificationItem>();

        /// <summary>
        /// Summary of verification issues found
        /// </summary>
        public List<VerificationIssue> Issues { get; set; } = new List<VerificationIssue>();

        /// <summary>
        /// Whether the verification was successful
        /// </summary>
        public bool IsSuccessful => Status == VerificationStatus.Successful;

        /// <summary>
        /// Number of resources that passed verification
        /// </summary>
        public int PassedCount => Resources.Count(r => r.Status == VerificationStatus.Successful);

        /// <summary>
        /// Number of resources that failed verification
        /// </summary>
        public int FailedCount => Resources.Count(r => r.Status == VerificationStatus.Failed);

        /// <summary>
        /// Number of resources with warnings
        /// </summary>
        public int WarningCount => Resources.Count(r => r.Status == VerificationStatus.Warning);
    }

    /// <summary>
    /// Status of resource verification
    /// </summary>
    public enum VerificationStatus
    {
        /// <summary>
        /// Verification was successful
        /// </summary>
        Successful,

        /// <summary>
        /// Verification found warnings but no critical issues
        /// </summary>
        Warning,

        /// <summary>
        /// Verification failed
        /// </summary>
        Failed,

        /// <summary>
        /// Verification could not be completed
        /// </summary>
        Error
    }
}
