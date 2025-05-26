using System;
using System.Threading.Tasks;
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

        // Add OpenAI model provider (you'll need to implement this)
        services.AddSingleton<IAIModelProvider, OpenAIModelProvider>();

        // Add script validator
        services.AddSingleton<IScriptValidator, BasicScriptValidator>();

        // Add infrastructure generator
        services.AddSingleton<IInfrastructureGenerator, OpenAIInfrastructureGenerator>();

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get logger and infrastructure generator
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var infrastructureGenerator = serviceProvider.GetRequiredService<IInfrastructureGenerator>();

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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating infrastructure script");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}