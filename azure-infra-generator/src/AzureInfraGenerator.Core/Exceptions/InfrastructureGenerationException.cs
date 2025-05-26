using System;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when infrastructure script generation fails
    /// </summary>
    public class InfrastructureGenerationException : Exception
    {
        /// <summary>
        /// The type of script that failed to generate
        /// </summary>
        public ScriptType ScriptType { get; }

        public InfrastructureGenerationException(
            string message, 
            ScriptType scriptType, 
            Exception innerException = null) 
            : base(message, innerException)
        {
            ScriptType = scriptType;
        }
    }
}
