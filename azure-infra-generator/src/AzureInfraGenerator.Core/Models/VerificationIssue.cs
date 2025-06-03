using System;

namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Issue found during resource verification
    /// </summary>
    public class VerificationIssue
    {
        /// <summary>
        /// Unique identifier for the issue
        /// </summary>
        public string IssueId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Resource ID associated with the issue
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Type of issue
        /// </summary>
        public IssueType Type { get; set; }

        /// <summary>
        /// Severity of the issue
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Description of the issue
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Recommended action to resolve the issue
        /// </summary>
        public string RecommendedAction { get; set; }

        /// <summary>
        /// When the issue was detected
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the issue is automatically fixable
        /// </summary>
        public bool IsAutoFixable { get; set; }

        /// <summary>
        /// Documentation link for more information
        /// </summary>
        public string DocumentationLink { get; set; }
    }

    /// <summary>
    /// Type of verification issue
    /// </summary>
    public enum IssueType
    {
        /// <summary>
        /// Configuration issue
        /// </summary>
        Configuration,

        /// <summary>
        /// Connectivity issue
        /// </summary>
        Connectivity,

        /// <summary>
        /// Performance issue
        /// </summary>
        Performance,

        /// <summary>
        /// Security issue
        /// </summary>
        Security,

        /// <summary>
        /// Compliance issue
        /// </summary>
        Compliance,

        /// <summary>
        /// Cost optimization issue
        /// </summary>
        CostOptimization,

        /// <summary>
        /// Availability issue
        /// </summary>
        Availability,

        /// <summary>
        /// Other type of issue
        /// </summary>
        Other
    }

    /// <summary>
    /// Severity of verification issue
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// Informational issue
        /// </summary>
        Info,

        /// <summary>
        /// Low severity issue
        /// </summary>
        Low,

        /// <summary>
        /// Medium severity issue
        /// </summary>
        Medium,

        /// <summary>
        /// High severity issue
        /// </summary>
        High,

        /// <summary>
        /// Critical issue
        /// </summary>
        Critical
    }
}
