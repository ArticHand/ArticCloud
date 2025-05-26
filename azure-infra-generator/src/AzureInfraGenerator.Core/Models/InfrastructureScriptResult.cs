using System;

namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Represents the result of infrastructure script generation
    /// </summary>
    public class InfrastructureScriptResult
    {
        /// <summary>
        /// The generated script content
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// The type of script generated
        /// </summary>
        public ScriptType ScriptType { get; set; }

        /// <summary>
        /// Timestamp of script generation
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// The original user prompt that generated the script
        /// </summary>
        public string OriginalPrompt { get; set; }

        /// <summary>
        /// The AI model used to generate the script
        /// </summary>
        public string ModelUsed { get; set; }
    }
}
