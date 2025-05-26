namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Represents available AI models for infrastructure generation
    /// </summary>
    public enum AIModel
    {
        /// <summary>
        /// OpenAI GPT-3.5 Turbo model
        /// </summary>
        GPT_3_5_Turbo,

        /// <summary>
        /// OpenAI GPT-4 model
        /// </summary>
        GPT_4,

        /// <summary>
        /// OpenAI GPT-4 Turbo model
        /// </summary>
        GPT_4_Turbo,

        /// <summary>
        /// Default model for infrastructure generation
        /// </summary>
        Default = GPT_4
    }
}
