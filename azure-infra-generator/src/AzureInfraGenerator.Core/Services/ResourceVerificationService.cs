using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Services
{
    /// <summary>
    /// Service for verifying and monitoring Azure resources after deployment
    /// </summary>
    public class ResourceVerificationService : IResourceVerificationService
    {
        private readonly ILogger<ResourceVerificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ArmClient _armClient;
        private readonly Dictionary<string, Dictionary<string, Func<GenericResource, Task<ResourceVerificationItem>>>> _resourceTypeVerifiers;

        /// <summary>
        /// Initializes a new instance of the Resource Verification Service
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="configuration">Configuration provider</param>
        public ResourceVerificationService(
            ILogger<ResourceVerificationService> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Initialize Azure ARM client with DefaultAzureCredential
            _armClient = new ArmClient(new DefaultAzureCredential());
            
            // Initialize resource type verifiers
            _resourceTypeVerifiers = InitializeResourceVerifiers();
            
            _logger.LogInformation("Resource Verification Service initialized");
        }

        /// <summary>
        /// Verifies the health and status of deployed resources
        /// </summary>
        public async Task<ResourceVerificationResult> VerifyResourcesAsync(DeploymentResult deploymentResult)
        {
            if (deploymentResult == null)
                throw new ArgumentNullException(nameof(deploymentResult));

            _logger.LogInformation($"Verifying resources for deployment '{deploymentResult.DeploymentId}'");

            var result = new ResourceVerificationResult
            {
                DeploymentId = deploymentResult.DeploymentId,
                VerificationTime = DateTime.UtcNow,
                Status = VerificationStatus.Successful
            };

            try
            {
                // Get subscription
                var subscription = await _armClient.GetDefaultSubscriptionAsync();
                
                // Get resource group
                var resourceGroup = await subscription.GetResourceGroupAsync(deploymentResult.ResourceGroupName);
                
                if (resourceGroup == null)
                {
                    _logger.LogWarning($"Resource group '{deploymentResult.ResourceGroupName}' not found");
                    result.Status = VerificationStatus.Failed;
                    result.Issues.Add(new VerificationIssue
                    {
                        Type = IssueType.Availability,
                        Severity = IssueSeverity.Critical,
                        Description = $"Resource group '{deploymentResult.ResourceGroupName}' not found",
                        RecommendedAction = "Check if the resource group was deleted or if you have proper permissions"
                    });
                    return result;
                }

                // Verify each resource
                foreach (var deployedResource in deploymentResult.Resources)
                {
                    var resourceVerification = await VerifyResourceAsync(deployedResource.ResourceId);
                    result.Resources.Add(resourceVerification);

                    // Add issues to the overall result
                    foreach (var issue in resourceVerification.Issues)
                    {
                        result.Issues.Add(issue);
                    }

                    // Update overall status based on resource verification
                    if (resourceVerification.Status == VerificationStatus.Failed && 
                        result.Status != VerificationStatus.Failed)
                    {
                        result.Status = VerificationStatus.Failed;
                    }
                    else if (resourceVerification.Status == VerificationStatus.Warning && 
                             result.Status == VerificationStatus.Successful)
                    {
                        result.Status = VerificationStatus.Warning;
                    }
                }

                _logger.LogInformation($"Resource verification completed with status: {result.Status}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resource verification failed");
                
                result.Status = VerificationStatus.Error;
                result.Issues.Add(new VerificationIssue
                {
                    Type = IssueType.Other,
                    Severity = IssueSeverity.Critical,
                    Description = $"Resource verification failed: {ex.Message}",
                    RecommendedAction = "Check Azure credentials and permissions"
                });
                
                return result;
            }
        }

        /// <summary>
        /// Verifies a specific resource by its ID
        /// </summary>
        public async Task<ResourceVerificationItem> VerifyResourceAsync(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("Resource ID cannot be empty", nameof(resourceId));

            _logger.LogInformation($"Verifying resource: {resourceId}");

            var verificationItem = new ResourceVerificationItem
            {
                ResourceId = resourceId,
                VerificationTime = DateTime.UtcNow,
                Status = VerificationStatus.Successful
            };

            try
            {
                // Parse resource ID to get resource type
                var resourceIdParts = resourceId.Split('/');
                if (resourceIdParts.Length < 8)
                {
                    _logger.LogWarning($"Invalid resource ID format: {resourceId}");
                    verificationItem.Status = VerificationStatus.Failed;
                    verificationItem.Issues.Add(new VerificationIssue
                    {
                        ResourceId = resourceId,
                        Type = IssueType.Other,
                        Severity = IssueSeverity.Critical,
                        Description = "Invalid resource ID format",
                        RecommendedAction = "Check the resource ID format"
                    });
                    return verificationItem;
                }

                // Extract resource information from ID
                var subscriptionId = resourceIdParts[2];
                var resourceGroupName = resourceIdParts[4];
                var providerName = resourceIdParts[6];
                var resourceTypeName = resourceIdParts[7];
                var resourceName = resourceIdParts.Length > 8 ? resourceIdParts[8] : "";

                // Set resource information
                verificationItem.Name = resourceName;
                verificationItem.ResourceType = $"{providerName}/{resourceTypeName}";
                verificationItem.PortalUrl = $"https://portal.azure.com/#@/resource{resourceId}";

                // Get resource
                var armResourceId = new ResourceIdentifier(resourceId);
                var resource = await _armClient.GetGenericResource(armResourceId).GetAsync();

                if (resource == null)
                {
                    _logger.LogWarning($"Resource not found: {resourceId}");
                    verificationItem.Status = VerificationStatus.Failed;
                    verificationItem.IsAccessible = false;
                    verificationItem.Issues.Add(new VerificationIssue
                    {
                        ResourceId = resourceId,
                        Type = IssueType.Availability,
                        Severity = IssueSeverity.Critical,
                        Description = "Resource not found",
                        RecommendedAction = "Check if the resource was deleted or if you have proper permissions"
                    });
                    return verificationItem;
                }

                // Resource exists, mark as accessible
                verificationItem.IsAccessible = true;
                verificationItem.ProvisioningState = resource.Value.Data.Properties?.ToString() ?? "Unknown";

                // Check provisioning state
                if (verificationItem.ProvisioningState != "Succeeded")
                {
                    _logger.LogWarning($"Resource is not in Succeeded state: {resourceId}, State: {verificationItem.ProvisioningState}");
                    verificationItem.Status = VerificationStatus.Warning;
                    verificationItem.Issues.Add(new VerificationIssue
                    {
                        ResourceId = resourceId,
                        Type = IssueType.Availability,
                        Severity = IssueSeverity.Medium,
                        Description = $"Resource is not in Succeeded state: {verificationItem.ProvisioningState}",
                        RecommendedAction = "Check resource provisioning status in Azure Portal"
                    });
                }

                // Perform resource-specific verification if available
                var resourceType = $"{providerName}/{resourceTypeName}";
                if (_resourceTypeVerifiers.TryGetValue(providerName, out var typeVerifiers) &&
                    typeVerifiers.TryGetValue(resourceTypeName, out var verifier))
                {
                    var specificVerification = await verifier(resource.Value);
                    
                    // Merge specific verification results
                    verificationItem.Status = specificVerification.Status;
                    verificationItem.Issues.AddRange(specificVerification.Issues);
                    verificationItem.Metrics = specificVerification.Metrics;
                    verificationItem.IsProperlyConfigured = specificVerification.IsProperlyConfigured;
                    verificationItem.HasSecurityIssues = specificVerification.HasSecurityIssues;
                }
                else
                {
                    // Basic verification for unsupported resource types
                    verificationItem.IsProperlyConfigured = true;
                    verificationItem.HasSecurityIssues = false;
                }

                _logger.LogInformation($"Resource verification completed for {resourceId} with status: {verificationItem.Status}");
                return verificationItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying resource: {resourceId}");
                
                verificationItem.Status = VerificationStatus.Error;
                verificationItem.IsAccessible = false;
                verificationItem.Issues.Add(new VerificationIssue
                {
                    ResourceId = resourceId,
                    Type = IssueType.Other,
                    Severity = IssueSeverity.Critical,
                    Description = $"Error verifying resource: {ex.Message}",
                    RecommendedAction = "Check Azure credentials and permissions"
                });
                
                return verificationItem;
            }
        }

        /// <summary>
        /// Monitors resources for a specified period to ensure stability
        /// </summary>
        public async Task<ResourceMonitoringResult> MonitorResourcesAsync(
            DeploymentResult deploymentResult, 
            int monitoringPeriodMinutes = 5)
        {
            if (deploymentResult == null)
                throw new ArgumentNullException(nameof(deploymentResult));

            if (monitoringPeriodMinutes <= 0)
                throw new ArgumentException("Monitoring period must be greater than zero", nameof(monitoringPeriodMinutes));

            _logger.LogInformation($"Starting resource monitoring for deployment '{deploymentResult.DeploymentId}' for {monitoringPeriodMinutes} minutes");

            var result = new ResourceMonitoringResult
            {
                DeploymentId = deploymentResult.DeploymentId,
                StartTime = DateTime.UtcNow,
                Status = MonitoringStatus.Stable
            };

            try
            {
                // Calculate monitoring end time
                var endTime = DateTime.UtcNow.AddMinutes(monitoringPeriodMinutes);
                
                // Create cancellation token that will cancel after the monitoring period
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(monitoringPeriodMinutes));
                
                // Monitor each resource
                var resourceIds = deploymentResult.Resources.Select(r => r.ResourceId).ToList();
                
                // Take initial snapshot
                await TakeMonitoringSnapshotAsync(result, resourceIds);
                
                // Monitor resources until the end time
                while (DateTime.UtcNow < endTime && !cts.Token.IsCancellationRequested)
                {
                    // Wait for monitoring interval (30 seconds)
                    await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
                    
                    // Take snapshot
                    await TakeMonitoringSnapshotAsync(result, resourceIds);
                    
                    // Check for issues
                    var latestDataPoints = result.DataPoints
                        .GroupBy(dp => dp.ResourceId)
                        .Select(g => g.OrderByDescending(dp => dp.Timestamp).First())
                        .ToList();
                    
                    var unavailableResources = latestDataPoints.Count(dp => !dp.IsAvailable);
                    
                    // Update monitoring status based on latest data
                    if (unavailableResources > 0)
                    {
                        if (unavailableResources == latestDataPoints.Count)
                        {
                            result.Status = MonitoringStatus.Unavailable;
                        }
                        else if (unavailableResources > latestDataPoints.Count / 2)
                        {
                            result.Status = MonitoringStatus.Unstable;
                        }
                        else
                        {
                            result.Status = MonitoringStatus.Degraded;
                        }
                    }
                }

                // Set end time
                result.EndTime = DateTime.UtcNow;
                
                _logger.LogInformation($"Resource monitoring completed with status: {result.Status}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resource monitoring failed");
                
                result.Status = MonitoringStatus.Failed;
                result.EndTime = DateTime.UtcNow;
                result.Issues.Add(new VerificationIssue
                {
                    Type = IssueType.Other,
                    Severity = IssueSeverity.Critical,
                    Description = $"Resource monitoring failed: {ex.Message}",
                    RecommendedAction = "Check Azure credentials and permissions"
                });
                
                return result;
            }
        }

        /// <summary>
        /// Takes a snapshot of resource status for monitoring
        /// </summary>
        private async Task TakeMonitoringSnapshotAsync(
            ResourceMonitoringResult monitoringResult, 
            List<string> resourceIds)
        {
            foreach (var resourceId in resourceIds)
            {
                try
                {
                    // Get resource
                    var armResourceId = new ResourceIdentifier(resourceId);
                    var resource = await _armClient.GetGenericResource(armResourceId).GetAsync();
                    
                    var dataPoint = new ResourceMonitoringDataPoint
                    {
                        ResourceId = resourceId,
                        Timestamp = DateTime.UtcNow,
                        Status = resource?.Value.Data.Properties?.ToString() ?? "Unknown",
                        IsAvailable = resource != null
                    };
                    
                    // Add basic metrics
                    if (resource != null)
                    {
                        // Add CPU usage metric if available (placeholder)
                        dataPoint.Metrics["ProvisioningState"] = resource.Value.Data.Properties?.ToString() == "Succeeded" ? 1.0 : 0.0;
                    }
                    
                    monitoringResult.DataPoints.Add(dataPoint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error taking monitoring snapshot for resource: {resourceId}");
                    
                    var dataPoint = new ResourceMonitoringDataPoint
                    {
                        ResourceId = resourceId,
                        Timestamp = DateTime.UtcNow,
                        Status = "Error",
                        IsAvailable = false
                    };
                    
                    monitoringResult.DataPoints.Add(dataPoint);
                    
                    monitoringResult.Issues.Add(new VerificationIssue
                    {
                        ResourceId = resourceId,
                        Type = IssueType.Availability,
                        Severity = IssueSeverity.Medium,
                        Description = $"Error monitoring resource: {ex.Message}",
                        RecommendedAction = "Check resource in Azure Portal"
                    });
                }
            }
        }

        /// <summary>
        /// Initializes resource-specific verifiers
        /// </summary>
        private Dictionary<string, Dictionary<string, Func<GenericResource, Task<ResourceVerificationItem>>>> InitializeResourceVerifiers()
        {
            var verifiers = new Dictionary<string, Dictionary<string, Func<GenericResource, Task<ResourceVerificationItem>>>>();

            // Microsoft.Compute verifiers
            verifiers["Microsoft.Compute"] = new Dictionary<string, Func<GenericResource, Task<ResourceVerificationItem>>>
            {
                // Virtual Machine verifier
                ["virtualMachines"] = VerifyVirtualMachineAsync
            };

            // Microsoft.Storage verifiers
            verifiers["Microsoft.Storage"] = new Dictionary<string, Func<GenericResource, Task<ResourceVerificationItem>>>
            {
                // Storage Account verifier
                ["storageAccounts"] = VerifyStorageAccountAsync
            };

            // Microsoft.Web verifiers
            verifiers["Microsoft.Web"] = new Dictionary<string, Func<GenericResource, Task<ResourceVerificationItem>>>
            {
                // App Service verifier
                ["sites"] = VerifyAppServiceAsync
            };

            // Microsoft.Sql verifiers
            verifiers["Microsoft.Sql"] = new Dictionary<string, Func<GenericResource, Task<ResourceVerificationItem>>>
            {
                // SQL Server verifier
                ["servers"] = VerifySqlServerAsync
            };

            return verifiers;
        }

        /// <summary>
        /// Verifies a Virtual Machine resource
        /// </summary>
        private async Task<ResourceVerificationItem> VerifyVirtualMachineAsync(GenericResource resource)
        {
            var result = new ResourceVerificationItem
            {
                ResourceId = resource.Data.Id,
                Name = resource.Data.Name,
                ResourceType = resource.Data.ResourceType,
                VerificationTime = DateTime.UtcNow,
                Status = VerificationStatus.Successful,
                ProvisioningState = resource.Data.Properties?.ToString() ?? "Unknown",
                IsAccessible = true,
                IsProperlyConfigured = true,
                HasSecurityIssues = false,
                PortalUrl = $"https://portal.azure.com/#@/resource{resource.Data.Id}"
            };

            // Check VM power state (simplified for implementation)
            var powerState = "Unknown";
            if (resource.Data.Properties != null)
            {
                var properties = resource.Data.Properties.ToString();
                if (properties.Contains("PowerState"))
                {
                    powerState = properties.Contains("running") ? "Running" : 
                                 properties.Contains("deallocated") ? "Deallocated" : 
                                 properties.Contains("stopped") ? "Stopped" : "Unknown";
                }
            }

            result.Metrics["PowerState"] = powerState;

            // Check VM status
            if (powerState != "Running")
            {
                result.Status = VerificationStatus.Warning;
                result.Issues.Add(new VerificationIssue
                {
                    ResourceId = result.ResourceId,
                    Type = IssueType.Availability,
                    Severity = IssueSeverity.Medium,
                    Description = $"Virtual Machine is not running (Current state: {powerState})",
                    RecommendedAction = "Start the VM if it should be running",
                    IsAutoFixable = true
                });
            }

            return result;
        }

        /// <summary>
        /// Verifies a Storage Account resource
        /// </summary>
        private async Task<ResourceVerificationItem> VerifyStorageAccountAsync(GenericResource resource)
        {
            var result = new ResourceVerificationItem
            {
                ResourceId = resource.Data.Id,
                Name = resource.Data.Name,
                ResourceType = resource.Data.ResourceType,
                VerificationTime = DateTime.UtcNow,
                Status = VerificationStatus.Successful,
                ProvisioningState = resource.Data.Properties?.ToString() ?? "Unknown",
                IsAccessible = true,
                IsProperlyConfigured = true,
                HasSecurityIssues = false,
                PortalUrl = $"https://portal.azure.com/#@/resource{resource.Data.Id}"
            };

            // Check if HTTPS is enabled (simplified for implementation)
            var httpsOnly = false;
            if (resource.Data.Properties != null)
            {
                var properties = resource.Data.Properties.ToString();
                httpsOnly = properties.Contains("supportsHttpsTrafficOnly") && 
                            properties.Contains("true");
            }

            result.Metrics["HttpsOnly"] = httpsOnly ? "Enabled" : "Disabled";

            // Check security
            if (!httpsOnly)
            {
                result.Status = VerificationStatus.Warning;
                result.HasSecurityIssues = true;
                result.Issues.Add(new VerificationIssue
                {
                    ResourceId = result.ResourceId,
                    Type = IssueType.Security,
                    Severity = IssueSeverity.Medium,
                    Description = "Storage Account does not enforce HTTPS only traffic",
                    RecommendedAction = "Enable 'Secure transfer required' in the storage account configuration",
                    IsAutoFixable = true,
                    DocumentationLink = "https://docs.microsoft.com/en-us/azure/storage/common/storage-require-secure-transfer"
                });
            }

            return result;
        }

        /// <summary>
        /// Verifies an App Service resource
        /// </summary>
        private async Task<ResourceVerificationItem> VerifyAppServiceAsync(GenericResource resource)
        {
            var result = new ResourceVerificationItem
            {
                ResourceId = resource.Data.Id,
                Name = resource.Data.Name,
                ResourceType = resource.Data.ResourceType,
                VerificationTime = DateTime.UtcNow,
                Status = VerificationStatus.Successful,
                ProvisioningState = resource.Data.Properties?.ToString() ?? "Unknown",
                IsAccessible = true,
                IsProperlyConfigured = true,
                HasSecurityIssues = false,
                PortalUrl = $"https://portal.azure.com/#@/resource{resource.Data.Id}"
            };

            // Check if HTTPS is enabled (simplified for implementation)
            var httpsOnly = false;
            if (resource.Data.Properties != null)
            {
                var properties = resource.Data.Properties.ToString();
                httpsOnly = properties.Contains("httpsOnly") && 
                            properties.Contains("true");
            }

            result.Metrics["HttpsOnly"] = httpsOnly ? "Enabled" : "Disabled";

            // Check security
            if (!httpsOnly)
            {
                result.Status = VerificationStatus.Warning;
                result.HasSecurityIssues = true;
                result.Issues.Add(new VerificationIssue
                {
                    ResourceId = result.ResourceId,
                    Type = IssueType.Security,
                    Severity = IssueSeverity.Medium,
                    Description = "App Service does not enforce HTTPS only traffic",
                    RecommendedAction = "Enable 'HTTPS Only' in the App Service configuration",
                    IsAutoFixable = true,
                    DocumentationLink = "https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https"
                });
            }

            return result;
        }

        /// <summary>
        /// Verifies a SQL Server resource
        /// </summary>
        private async Task<ResourceVerificationItem> VerifySqlServerAsync(GenericResource resource)
        {
            var result = new ResourceVerificationItem
            {
                ResourceId = resource.Data.Id,
                Name = resource.Data.Name,
                ResourceType = resource.Data.ResourceType,
                VerificationTime = DateTime.UtcNow,
                Status = VerificationStatus.Successful,
                ProvisioningState = resource.Data.Properties?.ToString() ?? "Unknown",
                IsAccessible = true,
                IsProperlyConfigured = true,
                HasSecurityIssues = false,
                PortalUrl = $"https://portal.azure.com/#@/resource{resource.Data.Id}"
            };

            // Check if firewall is enabled (simplified for implementation)
            var firewallEnabled = false;
            if (resource.Data.Properties != null)
            {
                var properties = resource.Data.Properties.ToString();
                firewallEnabled = properties.Contains("firewallRules") && 
                                 !properties.Contains("allowAllAzureIPs");
            }

            result.Metrics["FirewallEnabled"] = firewallEnabled ? "Enabled" : "Disabled";

            // Check security
            if (!firewallEnabled)
            {
                result.Status = VerificationStatus.Warning;
                result.HasSecurityIssues = true;
                result.Issues.Add(new VerificationIssue
                {
                    ResourceId = result.ResourceId,
                    Type = IssueType.Security,
                    Severity = IssueSeverity.High,
                    Description = "SQL Server firewall may allow broad access",
                    RecommendedAction = "Configure SQL Server firewall rules to restrict access",
                    IsAutoFixable = false,
                    DocumentationLink = "https://docs.microsoft.com/en-us/azure/azure-sql/database/firewall-configure"
                });
            }

            return result;
        }
    }
}
