using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Configuration;
using AzureInfraGenerator.Core.Models;
using AzureInfraGenerator.Core.Services;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Configure services
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(configure => 
        {
            configure.AddConsole();
            configure.AddConfiguration(configuration.GetSection("Logging"));
        });

        // Configure infrastructure generator
        services.Configure<InfrastructureGeneratorOptions>(
            configuration.GetSection("InfrastructureGenerator"));

        // Add Redis caching
        services.AddRedisCache(configuration["Redis:ConnectionString"]);

        // Add cache invalidation
        services.AddCacheInvalidation();

        // Register AI providers
        services.AddSingleton<OpenAIModelProvider>();
        services.AddSingleton<ClaudeModelProvider>();
        
        // Add AI provider factory
        services.AddSingleton<AIModelProviderFactory>();
        
        // Add AI provider using factory
        services.AddSingleton<IAIModelProvider>(provider => {
            var factory = provider.GetRequiredService<AIModelProviderFactory>();
            return factory.CreateProvider();
        });

        // Add script validator
        services.AddSingleton<IScriptValidator, BasicScriptValidator>();

        // Add infrastructure generator
        services.AddSingleton<IInfrastructureGenerator, InfrastructureGenerator>();

        // Add Azure deployment service
        services.AddSingleton<IAzureDeploymentService, AzureDeploymentService>();

        // Add resource verification service
        services.AddSingleton<IResourceVerificationService, ResourceVerificationService>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get services
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var infrastructureGenerator = serviceProvider.GetRequiredService<IInfrastructureGenerator>();
        var deploymentService = serviceProvider.GetRequiredService<IAzureDeploymentService>();
        var resourceVerificationService = serviceProvider.GetRequiredService<IResourceVerificationService>();

        try 
        {
            // Parse script type from configuration or default to Bicep
            var scriptTypeStr = configuration["ScriptType"] ?? "Bicep";
            var scriptType = Enum.Parse<ScriptType>(scriptTypeStr, true);

            // Interactive prompt
            Console.WriteLine("Azure Infrastructure Generator");
            Console.WriteLine("==============================");
            Console.Write("Enter your cloud infrastructure description: ");
            
            var userPrompt = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                logger.LogWarning("No prompt provided. Exiting.");
                return;
            }

            // Generate infrastructure script
            logger.LogInformation($"Generating {scriptType} script...");
            var result = await infrastructureGenerator.GenerateScriptAsync(userPrompt, scriptType);

            // Display results
            Console.WriteLine("\n--- Generated Script ---");
            Console.WriteLine(result.Script);

            Console.WriteLine("\n--- Metrics ---");
            Console.WriteLine($"Model Used: {result.ModelUsed}");
            Console.WriteLine($"Generated At: {result.GeneratedAt}");
            Console.WriteLine($"Generation Time: {result.Metrics.GenerationTime}");
            
            // Ask if user wants to deploy the generated script
            Console.WriteLine("\nDo you want to deploy this script to Azure? (y/n)");
            var deployResponse = Console.ReadLine()?.Trim().ToLower();
            
            if (deployResponse == "y" || deployResponse == "yes")
            {
                // Get deployment parameters
                Console.Write("Enter resource group name (leave empty for default): ");
                var resourceGroupName = Console.ReadLine()?.Trim();
                
                Console.Write("Enter Azure region (leave empty for default): ");
                var location = Console.ReadLine()?.Trim();
                
                Console.Write("Enter deployment name (leave empty for auto-generated): ");
                var deploymentName = Console.ReadLine()?.Trim();
                
                // Deploy the script
                logger.LogInformation("Deploying infrastructure to Azure...");
                Console.WriteLine("\nDeploying to Azure...");
                
                var deploymentResult = await deploymentService.DeployAsync(
                    result.Script,
                    scriptType,
                    deploymentName,
                    resourceGroupName,
                    location);
                
                // Check deployment status
                while (deploymentResult.Status == DeploymentStatus.Preparing || 
                       deploymentResult.Status == DeploymentStatus.Running)
                {
                    Console.Write(".");
                    await Task.Delay(5000); // Wait 5 seconds between status checks
                    await deploymentService.GetDeploymentStatusAsync(deploymentResult.DeploymentId);
                }
                
                Console.WriteLine();
                
                // Show deployment results
                Console.WriteLine($"\n--- Deployment Results ---");
                Console.WriteLine($"Status: {deploymentResult.Status}");
                Console.WriteLine($"Deployment ID: {deploymentResult.DeploymentId}");
                Console.WriteLine($"Duration: {deploymentResult.Duration}");
                
                if (deploymentResult.IsSuccessful)
                {
                    Console.WriteLine($"Resources created: {deploymentResult.Resources.Count}");
                    
                    // Verify resources if deployment was successful
                    Console.WriteLine("\nVerifying deployed resources...");
                    var verificationResult = await resourceVerificationService.VerifyResourcesAsync(deploymentResult);
                    
                    Console.WriteLine($"\n--- Resource Verification Results ---");
                    Console.WriteLine($"Status: {verificationResult.Status}");
                    Console.WriteLine($"Resources verified: {verificationResult.Resources.Count}");
                    Console.WriteLine($"Passed: {verificationResult.PassedCount}");
                    Console.WriteLine($"Warnings: {verificationResult.WarningCount}");
                    Console.WriteLine($"Failed: {verificationResult.FailedCount}");
                    
                    // Show issues if any
                    if (verificationResult.Issues.Count > 0)
                    {
                        Console.WriteLine("\nIssues found:");
                        foreach (var issue in verificationResult.Issues)
                        {
                            Console.WriteLine($"- [{issue.Severity}] {issue.Description}");
                            Console.WriteLine($"  Recommendation: {issue.RecommendedAction}");
                        }
                    }
                    
                    // Ask if user wants to monitor resources
                    Console.WriteLine("\nDo you want to monitor the deployed resources for stability? (y/n)");
                    var monitorResponse = Console.ReadLine()?.Trim().ToLower();
                    
                    if (monitorResponse == "y" || monitorResponse == "yes")
                    {
                        Console.Write("Enter monitoring period in minutes (default: 5): ");
                        var periodInput = Console.ReadLine()?.Trim();
                        var monitoringPeriod = 5;
                        
                        if (!string.IsNullOrEmpty(periodInput) && int.TryParse(periodInput, out var period))
                        {
                            monitoringPeriod = period;
                        }
                        
                        Console.WriteLine($"\nMonitoring resources for {monitoringPeriod} minutes...");
                        var monitoringResult = await resourceVerificationService.MonitorResourcesAsync(
                            deploymentResult, 
                            monitoringPeriod);
                        
                        Console.WriteLine($"\n--- Monitoring Results ---");
                        Console.WriteLine($"Status: {monitoringResult.Status}");
                        Console.WriteLine($"Duration: {monitoringResult.Duration}");
                        Console.WriteLine($"Data points collected: {monitoringResult.DataPoints.Count}");
                        
                        if (monitoringResult.Issues.Count > 0)
                        {
                            Console.WriteLine("\nIssues detected during monitoring:");
                            foreach (var issue in monitoringResult.Issues)
                            {
                                Console.WriteLine($"- [{issue.Severity}] {issue.Description}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {deploymentResult.ErrorMessage}");
                }
                
                // Show portal URL if available
                if (!string.IsNullOrEmpty(deploymentResult.PortalUrl))
                {
                    Console.WriteLine($"\nView in Azure Portal: {deploymentResult.PortalUrl}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating infrastructure script");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}