using System;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Defines a contract for calculating generation costs
    /// </summary>
    public interface ICostCalculator
    {
        /// <summary>
        /// Calculates the cost for a specific AI model and token usage
        /// </summary>
        /// <param name="model">AI Model used</param>
        /// <param name="tokensUsed">Number of tokens consumed</param>
        /// <returns>Estimated cost in USD</returns>
        decimal CalculateCost(AIModel model, int tokensUsed);
    }

    /// <summary>
    /// Default implementation of cost calculation
    /// </summary>
    public class OpenAICostCalculator : ICostCalculator
    {
        /// <summary>
        /// Calculates cost based on OpenAI pricing model
        /// </summary>
        public decimal CalculateCost(AIModel model, int tokensUsed)
        {
            return model switch
            {
                AIModel.GPT_3_5_Turbo => tokensUsed * 0.0015m / 1000,   // $0.0015 per 1K tokens
                AIModel.GPT_4 => tokensUsed * 0.03m / 1000,             // $0.03 per 1K tokens
                AIModel.GPT_4_Turbo => tokensUsed * 0.01m / 1000,       // $0.01 per 1K tokens
                _ => 0
            };
        }
    }
}
