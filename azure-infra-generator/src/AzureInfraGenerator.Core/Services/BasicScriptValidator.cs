using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AzureInfraGenerator.Core.Abstractions;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Services
{
    /// <summary>
    /// Basic implementation of script validator
    /// </summary>
    public class BasicScriptValidator : IScriptValidator
    {
        private readonly ILogger<BasicScriptValidator> _logger;

        public BasicScriptValidator(ILogger<BasicScriptValidator> logger)
        {
            _logger = logger ?? 
                throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates a generated script
        /// </summary>
        public async Task<ScriptValidationResult> ValidateScriptAsync(
            string script, 
            ScriptType scriptType)
        {
            var result = new ScriptValidationResult { IsValid = true };

            try 
            {
                // Basic validation checks
                if (string.IsNullOrWhiteSpace(script))
                {
                    result.IsValid = false;
                    result.Issues.Add(new ScriptIssue
                    {
                        Severity = ScriptIssueSeverity.Error,
                        Description = "Script cannot be empty"
                    });
                }

                // Script type specific validations
                switch (scriptType)
                {
                    case ScriptType.Bicep:
                        ValidateBicepScript(script, result);
                        break;
                    case ScriptType.Terraform:
                        ValidateTerraformScript(script, result);
                        break;
                    case ScriptType.PowerShell:
                        ValidatePowerShellScript(script, result);
                        break;
                }

                // Log validation results
                if (!result.IsValid)
                {
                    _logger.LogWarning($"Script validation found {result.Issues.Count} issues");
                    foreach (var issue in result.Issues)
                    {
                        _logger.LogWarning($"Issue: {issue.Description} (Severity: {issue.Severity})");
                    }
                }

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during script validation");
                
                result.IsValid = false;
                result.Issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Error,
                    Description = $"Validation error: {ex.Message}"
                });

                return result;
            }
        }

        private void ValidateBicepScript(string script, ScriptValidationResult result)
        {
            // Basic Bicep validation
            if (!script.Contains("resource"))
            {
                result.Issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Warning,
                    Description = "No resources defined in Bicep script"
                });
            }

            // Check for basic syntax
            if (!script.Contains("param") && !script.Contains("var"))
            {
                result.Issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Information,
                    Description = "No parameters or variables defined"
                });
            }
        }

        private void ValidateTerraformScript(string script, ScriptValidationResult result)
        {
            // Basic Terraform validation
            if (!script.Contains("provider"))
            {
                result.Issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Warning,
                    Description = "No provider block found in Terraform script"
                });
            }

            // Check for resource blocks
            if (!script.Contains("resource"))
            {
                result.Issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Error,
                    Description = "No resources defined in Terraform script"
                });
            }
        }

        private void ValidatePowerShellScript(string script, ScriptValidationResult result)
        {
            // Basic PowerShell validation
            if (!script.Contains("Azure"))
            {
                result.Issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Warning,
                    Description = "No Azure-specific cmdlets detected"
                });
            }

            // Check for error handling
            if (!script.Contains("try") && !script.Contains("catch"))
            {
                result.Issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Information,
                    Description = "No error handling detected"
                });
            }
        }
    }
}
