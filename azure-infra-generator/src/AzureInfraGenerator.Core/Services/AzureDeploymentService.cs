using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Models;
using AzureInfraGenerator.Core.Exceptions;

namespace AzureInfraGenerator.Core.Services
{
    /// <summary>
    /// Service for deploying infrastructure scripts to Azure
    /// </summary>
    public class AzureDeploymentService : IAzureDeploymentService
    {
        private readonly ILogger<AzureDeploymentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ArmClient _armClient;
        private readonly Dictionary<string, DeploymentResult> _deploymentCache = new Dictionary<string, DeploymentResult>();

        /// <summary>
        /// Initializes a new instance of the Azure Deployment Service
        /// </summary>
        /// <param name="logger">Logging service</param>
        /// <param name="configuration">Configuration provider</param>
        public AzureDeploymentService(
            ILogger<AzureDeploymentService> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Initialize Azure ARM client with DefaultAzureCredential
            // This will try multiple authentication methods in sequence
            _armClient = new ArmClient(new DefaultAzureCredential());
            
            _logger.LogInformation("Azure Deployment Service initialized");
        }

        /// <summary>
        /// Deploys an infrastructure script to Azure
        /// </summary>
        public async Task<DeploymentResult> DeployAsync(
            string script, 
            ScriptType scriptType, 
            string deploymentName = null, 
            string resourceGroupName = null, 
            string location = null)
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentException("Script cannot be empty", nameof(script));

            // Generate deployment name if not provided
            deploymentName ??= $"deployment-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            
            // Use default resource group from config if not provided
            resourceGroupName ??= _configuration["Azure:DefaultResourceGroup"] ?? 
                throw new InvalidOperationException("Resource group name is required but not provided");
            
            // Use default location from config if not provided
            location ??= _configuration["Azure:DefaultLocation"] ?? "eastus";

            _logger.LogInformation($"Starting {scriptType} deployment '{deploymentName}' to resource group '{resourceGroupName}'");

            var result = new DeploymentResult
            {
                DeploymentId = Guid.NewGuid().ToString(),
                DeploymentName = deploymentName,
                Status = DeploymentStatus.Preparing,
                StartTime = DateTime.UtcNow,
                ResourceGroupName = resourceGroupName,
                Location = location,
                ScriptType = scriptType
            };

            // Cache the deployment result for status tracking
            _deploymentCache[result.DeploymentId] = result;

            try
            {
                // Get subscription from the first available subscription
                var subscription = await _armClient.GetDefaultSubscriptionAsync();
                
                // Ensure resource group exists
                var resourceGroup = await EnsureResourceGroupExistsAsync(subscription, resourceGroupName, location);

                // Deploy based on script type
                switch (scriptType)
                {
                    case ScriptType.Bicep:
                        await DeployBicepScriptAsync(resourceGroup, script, deploymentName, result);
                        break;
                    
                    case ScriptType.Terraform:
                        await DeployTerraformScriptAsync(resourceGroup, script, deploymentName, result);
                        break;
                    
                    case ScriptType.PowerShell:
                        throw new NotSupportedException("PowerShell deployment is not currently supported");
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(scriptType), "Unsupported script type");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Deployment failed: {ex.Message}");
                
                result.Status = DeploymentStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.DetailedError = ex.ToString();
                result.EndTime = DateTime.UtcNow;
                
                return result;
            }
        }

        /// <summary>
        /// Gets the status of an ongoing or completed deployment
        /// </summary>
        public async Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId)
        {
            if (string.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Deployment ID cannot be empty", nameof(deploymentId));

            if (!_deploymentCache.TryGetValue(deploymentId, out var deployment))
            {
                _logger.LogWarning($"Deployment with ID '{deploymentId}' not found");
                return DeploymentStatus.Unknown;
            }

            // If deployment is already in a terminal state, return current status
            if (deployment.Status == DeploymentStatus.Succeeded || 
                deployment.Status == DeploymentStatus.Failed ||
                deployment.Status == DeploymentStatus.Canceled)
            {
                return deployment.Status;
            }

            try
            {
                // Get subscription
                var subscription = await _armClient.GetDefaultSubscriptionAsync();
                
                // Get resource group
                var resourceGroup = await subscription.GetResourceGroupAsync(deployment.ResourceGroupName);
                
                if (resourceGroup == null)
                {
                    _logger.LogWarning($"Resource group '{deployment.ResourceGroupName}' not found");
                    deployment.Status = DeploymentStatus.Failed;
                    deployment.ErrorMessage = $"Resource group '{deployment.ResourceGroupName}' not found";
                    deployment.EndTime = DateTime.UtcNow;
                    return deployment.Status;
                }

                // Get deployment
                var armDeployment = await resourceGroup.Value.GetArmDeploymentAsync(deployment.DeploymentName);
                
                if (armDeployment == null)
                {
                    _logger.LogWarning($"Deployment '{deployment.DeploymentName}' not found in resource group '{deployment.ResourceGroupName}'");
                    deployment.Status = DeploymentStatus.Failed;
                    deployment.ErrorMessage = $"Deployment '{deployment.DeploymentName}' not found";
                    deployment.EndTime = DateTime.UtcNow;
                    return deployment.Status;
                }

                // Update deployment status based on ARM deployment state
                var armDeploymentState = armDeployment.Value.Data.Properties.ProvisioningState.ToString();
                
                deployment.Status = armDeploymentState switch
                {
                    "Succeeded" => DeploymentStatus.Succeeded,
                    "Failed" => DeploymentStatus.Failed,
                    "Canceled" => DeploymentStatus.Canceled,
                    "Running" or "Accepted" => DeploymentStatus.Running,
                    _ => DeploymentStatus.Unknown
                };

                // If deployment is complete, update end time and resources
                if (deployment.Status == DeploymentStatus.Succeeded || 
                    deployment.Status == DeploymentStatus.Failed ||
                    deployment.Status == DeploymentStatus.Canceled)
                {
                    deployment.EndTime = DateTime.UtcNow;
                    
                    // If successful, update deployed resources
                    if (deployment.Status == DeploymentStatus.Succeeded)
                    {
                        await UpdateDeployedResourcesAsync(deployment, resourceGroup.Value);
                    }
                    // If failed, capture error details
                    else if (deployment.Status == DeploymentStatus.Failed && 
                             armDeployment.Value.Data.Properties.Error != null)
                    {
                        deployment.ErrorMessage = armDeployment.Value.Data.Properties.Error.Message;
                        deployment.DetailedError = armDeployment.Value.Data.Properties.Error.Details?.ToString();
                    }
                }

                // Generate Azure Portal URL for the deployment
                var subscriptionId = subscription.Data.SubscriptionId;
                deployment.PortalUrl = $"https://portal.azure.com/#@/resource/subscriptions/{subscriptionId}/resourceGroups/{deployment.ResourceGroupName}/providers/Microsoft.Resources/deployments/{deployment.DeploymentName}";

                return deployment.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking deployment status: {ex.Message}");
                return deployment.Status;
            }
        }

        /// <summary>
        /// Cancels an ongoing deployment
        /// </summary>
        public async Task<bool> CancelDeploymentAsync(string deploymentId)
        {
            if (string.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Deployment ID cannot be empty", nameof(deploymentId));

            if (!_deploymentCache.TryGetValue(deploymentId, out var deployment))
            {
                _logger.LogWarning($"Deployment with ID '{deploymentId}' not found");
                return false;
            }

            // If deployment is already in a terminal state, it cannot be canceled
            if (deployment.Status == DeploymentStatus.Succeeded || 
                deployment.Status == DeploymentStatus.Failed ||
                deployment.Status == DeploymentStatus.Canceled)
            {
                _logger.LogWarning($"Deployment '{deploymentId}' is already in state '{deployment.Status}' and cannot be canceled");
                return false;
            }

            try
            {
                // Get subscription
                var subscription = await _armClient.GetDefaultSubscriptionAsync();
                
                // Get resource group
                var resourceGroup = await subscription.GetResourceGroupAsync(deployment.ResourceGroupName);
                
                if (resourceGroup == null)
                {
                    _logger.LogWarning($"Resource group '{deployment.ResourceGroupName}' not found");
                    return false;
                }

                // Get deployment
                var armDeployment = await resourceGroup.Value.GetArmDeploymentAsync(deployment.DeploymentName);
                
                if (armDeployment == null)
                {
                    _logger.LogWarning($"Deployment '{deployment.DeploymentName}' not found in resource group '{deployment.ResourceGroupName}'");
                    return false;
                }

                // Cancel the deployment
                await armDeployment.Value.CancelAsync();
                
                // Update deployment status
                deployment.Status = DeploymentStatus.Canceled;
                deployment.EndTime = DateTime.UtcNow;
                
                _logger.LogInformation($"Deployment '{deploymentId}' canceled successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error canceling deployment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ensures the specified resource group exists, creating it if necessary
        /// </summary>
        private async Task<ResourceGroupResource> EnsureResourceGroupExistsAsync(
            SubscriptionResource subscription, 
            string resourceGroupName, 
            string location)
        {
            // Check if resource group exists
            var resourceGroups = subscription.GetResourceGroups();
            var existingGroup = await resourceGroups.GetIfExistsAsync(resourceGroupName);
            
            if (existingGroup != null)
            {
                _logger.LogInformation($"Using existing resource group '{resourceGroupName}'");
                return existingGroup;
            }

            // Create resource group if it doesn't exist
            _logger.LogInformation($"Creating resource group '{resourceGroupName}' in location '{location}'");
            
            var parameters = new ResourceGroupData(location);
            var operation = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, parameters);
            
            return operation.Value;
        }

        /// <summary>
        /// Deploys a Bicep script to Azure
        /// </summary>
        private async Task DeployBicepScriptAsync(
            ResourceGroupResource resourceGroup, 
            string bicepScript, 
            string deploymentName, 
            DeploymentResult result)
        {
            // Save Bicep script to temporary file
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            var bicepFilePath = Path.Combine(tempDir, "main.bicep");
            await File.WriteAllTextAsync(bicepFilePath, bicepScript);

            try
            {
                // Compile Bicep to ARM template
                _logger.LogInformation("Compiling Bicep to ARM template");
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "az",
                    Arguments = $"bicep build --file \"{bicepFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(startInfo);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Bicep compilation failed: {error}");
                }

                // Read compiled ARM template
                var armTemplatePath = Path.ChangeExtension(bicepFilePath, "json");
                var armTemplateContent = await File.ReadAllTextAsync(armTemplatePath);

                // Deploy ARM template
                result.Status = DeploymentStatus.Running;
                
                _logger.LogInformation("Deploying ARM template");
                
                var deploymentContent = new ArmDeploymentContent
                {
                    Properties = new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
                    {
                        Template = BinaryData.FromString(armTemplateContent)
                    }
                };

                var operation = await resourceGroup.GetArmDeployments().CreateOrUpdateAsync(
                    WaitUntil.Started, 
                    deploymentName, 
                    deploymentContent);

                _logger.LogInformation($"Deployment '{deploymentName}' started successfully");
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temporary files");
                }
            }
        }

        /// <summary>
        /// Deploys a Terraform script to Azure
        /// </summary>
        private async Task DeployTerraformScriptAsync(
            ResourceGroupResource resourceGroup, 
            string terraformScript, 
            string deploymentName, 
            DeploymentResult result)
        {
            // Create temporary directory for Terraform files
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            var mainTfPath = Path.Combine(tempDir, "main.tf");
            await File.WriteAllTextAsync(mainTfPath, terraformScript);

            // Create backend.tf for state management
            var backendTfPath = Path.Combine(tempDir, "backend.tf");
            var backendConfig = @"
terraform {
  backend ""azurerm"" {
    resource_group_name  = ""terraform-state""
    storage_account_name = ""tfstate" + Guid.NewGuid().ToString("N").Substring(0, 8) + @"""
    container_name       = ""tfstate""
    key                  = ""terraform.tfstate""
  }
}";
            await File.WriteAllTextAsync(backendTfPath, backendConfig);

            // Create providers.tf
            var providersTfPath = Path.Combine(tempDir, "providers.tf");
            var providersConfig = @"
provider ""azurerm"" {
  features {}
}";
            await File.WriteAllTextAsync(providersTfPath, providersConfig);

            try
            {
                result.Status = DeploymentStatus.Running;

                // Initialize Terraform
                _logger.LogInformation("Initializing Terraform");
                
                var initProcess = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "terraform",
                    Arguments = "init",
                    WorkingDirectory = tempDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(initProcess);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Terraform initialization failed: {error}");
                }

                // Apply Terraform configuration
                _logger.LogInformation("Applying Terraform configuration");
                
                var applyProcess = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "terraform",
                    Arguments = "apply -auto-approve",
                    WorkingDirectory = tempDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process = System.Diagnostics.Process.Start(applyProcess);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Terraform apply failed: {error}");
                }

                _logger.LogInformation($"Terraform deployment '{deploymentName}' completed successfully");

                // Since Terraform doesn't create an ARM deployment, we'll create a custom one to track it
                var deploymentContent = new ArmDeploymentContent
                {
                    Properties = new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
                    {
                        Template = BinaryData.FromString(@"{""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"", ""contentVersion"": ""1.0.0.0"", ""resources"": []}"),
                        Parameters = BinaryData.FromString(@"{""terraformDeployment"":{""value"":""" + deploymentName + @"""}}"),
                        DeploymentDebugLogLevel = DeploymentDebugLogLevel.None
                    }
                };

                await resourceGroup.GetArmDeployments().CreateOrUpdateAsync(
                    WaitUntil.Completed, 
                    deploymentName, 
                    deploymentContent);
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temporary files");
                }
            }
        }

        /// <summary>
        /// Updates the list of deployed resources in the deployment result
        /// </summary>
        private async Task UpdateDeployedResourcesAsync(
            DeploymentResult deployment, 
            ResourceGroupResource resourceGroup)
        {
            try
            {
                // Get all resources in the resource group
                var resources = resourceGroup.GetGenericResources();
                
                await foreach (var resource in resources)
                {
                    deployment.Resources.Add(new DeployedResource
                    {
                        ResourceId = resource.Data.Id,
                        Name = resource.Data.Name,
                        ResourceType = resource.Data.ResourceType,
                        ProvisioningState = resource.Data.Properties?.ToString() ?? "Unknown",
                        Timestamp = DateTime.UtcNow,
                        IsNewResource = true, // Assuming all resources are new for simplicity
                        PortalUrl = $"https://portal.azure.com/#@/resource{resource.Data.Id}"
                    });
                }

                _logger.LogInformation($"Updated deployment with {deployment.Resources.Count} resources");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update deployed resources");
            }
        }
    }
}
