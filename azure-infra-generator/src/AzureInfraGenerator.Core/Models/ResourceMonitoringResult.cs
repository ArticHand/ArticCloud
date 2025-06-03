using System;
using System.Collections.Generic;

namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Result of resource monitoring process
    /// </summary>
    public class ResourceMonitoringResult
    {
        /// <summary>
        /// ID of the deployment that was monitored
        /// </summary>
        public string DeploymentId { get; set; }

        /// <summary>
        /// When monitoring started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// When monitoring ended
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of monitoring
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Overall monitoring status
        /// </summary>
        public MonitoringStatus Status { get; set; }

        /// <summary>
        /// Monitoring data points collected
        /// </summary>
        public List<ResourceMonitoringDataPoint> DataPoints { get; set; } = new List<ResourceMonitoringDataPoint>();

        /// <summary>
        /// Issues detected during monitoring
        /// </summary>
        public List<VerificationIssue> Issues { get; set; } = new List<VerificationIssue>();

        /// <summary>
        /// Performance metrics collected during monitoring
        /// </summary>
        public Dictionary<string, List<MetricDataPoint>> Metrics { get; set; } = new Dictionary<string, List<MetricDataPoint>>();

        /// <summary>
        /// Whether the monitoring was successful
        /// </summary>
        public bool IsSuccessful => Status == MonitoringStatus.Stable;
    }

    /// <summary>
    /// Status of resource monitoring
    /// </summary>
    public enum MonitoringStatus
    {
        /// <summary>
        /// Resources are stable
        /// </summary>
        Stable,

        /// <summary>
        /// Resources have degraded performance
        /// </summary>
        Degraded,

        /// <summary>
        /// Resources are unstable
        /// </summary>
        Unstable,

        /// <summary>
        /// Resources are unavailable
        /// </summary>
        Unavailable,

        /// <summary>
        /// Monitoring failed
        /// </summary>
        Failed
    }

    /// <summary>
    /// Data point collected during resource monitoring
    /// </summary>
    public class ResourceMonitoringDataPoint
    {
        /// <summary>
        /// Resource ID being monitored
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Timestamp of the data point
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Status of the resource at this point
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Metrics collected at this point
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Whether the resource was available at this point
        /// </summary>
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// Metric data point
    /// </summary>
    public class MetricDataPoint
    {
        /// <summary>
        /// Timestamp of the metric
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Value of the metric
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Unit of the metric
        /// </summary>
        public string Unit { get; set; }
    }
}
