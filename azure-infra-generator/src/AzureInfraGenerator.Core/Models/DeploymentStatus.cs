namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Status of an Azure deployment
    /// </summary>
    public enum DeploymentStatus
    {
        /// <summary>
        /// Deployment is being prepared
        /// </summary>
        Preparing,

        /// <summary>
        /// Deployment is in progress
        /// </summary>
        Running,

        /// <summary>
        /// Deployment has completed successfully
        /// </summary>
        Succeeded,

        /// <summary>
        /// Deployment has failed
        /// </summary>
        Failed,

        /// <summary>
        /// Deployment was canceled
        /// </summary>
        Canceled,

        /// <summary>
        /// Deployment status is unknown
        /// </summary>
        Unknown
    }
}
