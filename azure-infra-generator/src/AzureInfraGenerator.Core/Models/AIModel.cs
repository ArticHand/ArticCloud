namespace AzureInfraGenerator.Core.Models
{
    /// <summary>
    /// Represents available AI models for infrastructure generation
    /// </summary>
    public enum AIModel
    {
        // OpenAI Models
        
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
        
        // Anthropic Claude Models
        
        /// <summary>
        /// Anthropic Claude 3 Opus model - most capable Claude model
        /// </summary>
        Claude_3_Opus,
        
        /// <summary>
        /// Anthropic Claude 3 Sonnet model - balanced performance and cost
        /// </summary>
        Claude_3_Sonnet,
        
        /// <summary>
        /// Anthropic Claude 3 Haiku model - fastest and most efficient Claude model
        /// </summary>
        Claude_3_Haiku,

        /// <summary>
        /// Default model for infrastructure generation
        /// </summary>
        Default = GPT_4
    }
}
