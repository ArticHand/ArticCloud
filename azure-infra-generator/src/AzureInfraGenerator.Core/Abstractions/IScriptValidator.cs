using System.Collections.Generic;
using System.Threading.Tasks;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Represents a script validation issue
    /// </summary>
    public class ScriptIssue
    {
        /// <summary>
        /// Severity of the issue
        /// </summary>
        public ScriptIssueSeverity Severity { get; set; }

        /// <summary>
        /// Description of the issue
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Line number where the issue occurs (if applicable)
        /// </summary>
        public int? LineNumber { get; set; }
    }

    /// <summary>
    /// Severity levels for script issues
    /// </summary>
    public enum ScriptIssueSeverity
    {
        Information,
        Warning,
        Error
    }

    /// <summary>
    /// Represents the result of script validation
    /// </summary>
    public class ScriptValidationResult
    {
        /// <summary>
        /// Indicates if the script is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of issues found during validation
        /// </summary>
        public List<ScriptIssue> Issues { get; set; } = new();
    }

    /// <summary>
    /// Defines a contract for validating infrastructure scripts
    /// </summary>
    public interface IScriptValidator
    {
        /// <summary>
        /// Validates a generated script
        /// </summary>
        /// <param name="script">Script content to validate</param>
        /// <param name="scriptType">Type of script</param>
        /// <returns>Validation result</returns>
        Task<ScriptValidationResult> ValidateScriptAsync(string script, ScriptType scriptType);
    }

    /// <summary>
    /// Basic implementation of script validator
    /// </summary>
    public class BasicScriptValidator : IScriptValidator
    {
        public async Task<ScriptValidationResult> ValidateScriptAsync(string script, ScriptType scriptType)
        {
            var result = new ScriptValidationResult { IsValid = true };

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
                    result.Issues.AddRange(ValidateBicepScript(script));
                    break;
                case ScriptType.Terraform:
                    result.Issues.AddRange(ValidateTerraformScript(script));
                    break;
                case ScriptType.PowerShell:
                    result.Issues.AddRange(ValidatePowerShellScript(script));
                    break;
            }

            result.IsValid = result.Issues.All(i => i.Severity != ScriptIssueSeverity.Error);
            return await Task.FromResult(result);
        }

        private List<ScriptIssue> ValidateBicepScript(string script)
        {
            var issues = new List<ScriptIssue>();
            
            // Example basic Bicep validation
            if (!script.Contains("resource"))
            {
                issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Warning,
                    Description = "No resources defined in Bicep script"
                });
            }

            return issues;
        }

        private List<ScriptIssue> ValidateTerraformScript(string script)
        {
            var issues = new List<ScriptIssue>();
            
            // Example basic Terraform validation
            if (!script.Contains("provider"))
            {
                issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Warning,
                    Description = "No provider block found in Terraform script"
                });
            }

            return issues;
        }

        private List<ScriptIssue> ValidatePowerShellScript(string script)
        {
            var issues = new List<ScriptIssue>();
            
            // Example basic PowerShell validation
            if (!script.Contains("Azure"))
            {
                issues.Add(new ScriptIssue
                {
                    Severity = ScriptIssueSeverity.Warning,
                    Description = "No Azure-specific cmdlets detected"
                });
            }

            return issues;
        }
    }
}
