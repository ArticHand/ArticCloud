using System;
using System.Threading.Tasks;
using AzureInfraGenerator.Core.Models;

namespace AzureInfraGenerator.Core.Abstractions
{
    /// <summary>
    /// Defines a contract for caching infrastructure scripts
    /// </summary>
    public interface IScriptCache
    {
        /// <summary>
        /// Attempts to retrieve a cached script
        /// </summary>
        /// <param name="prompt">Original user prompt</param>
        /// <param name="scriptType">Type of script</param>
        /// <returns>Cached script or null if not found</returns>
        Task<string> GetCachedScriptAsync(string prompt, ScriptType scriptType);

        /// <summary>
        /// Caches a generated script
        /// </summary>
        /// <param name="prompt">Original user prompt</param>
        /// <param name="scriptType">Type of script</param>
        /// <param name="script">Generated script content</param>
        /// <returns>Task representing the cache operation</returns>
        Task CacheScriptAsync(string prompt, ScriptType scriptType, string script);

        /// <summary>
        /// Checks if a script is already cached
        /// </summary>
        /// <param name="prompt">Original user prompt</param>
        /// <param name="scriptType">Type of script</param>
        /// <returns>True if cached, false otherwise</returns>
        Task<bool> IsCachedAsync(string prompt, ScriptType scriptType);

        /// <summary>
        /// Removes a specific script from cache
        /// </summary>
        /// <param name="prompt">Original user prompt</param>
        /// <param name="scriptType">Type of script</param>
        /// <returns>Task representing the cache removal operation</returns>
        Task InvalidateCacheAsync(string prompt, ScriptType scriptType);
    }

    /// <summary>
    /// Default in-memory implementation of script cache
    /// </summary>
    public class InMemoryScriptCache : IScriptCache
    {
        private readonly Dictionary<string, string> _scriptCache = new();

        private string GenerateCacheKey(string prompt, ScriptType scriptType) =>
            $"{scriptType}:{prompt.GetHashCode()}";

        public Task<string> GetCachedScriptAsync(string prompt, ScriptType scriptType)
        {
            var key = GenerateCacheKey(prompt, scriptType);
            return Task.FromResult(_scriptCache.TryGetValue(key, out var cachedScript) ? cachedScript : null);
        }

        public Task CacheScriptAsync(string prompt, ScriptType scriptType, string script)
        {
            var key = GenerateCacheKey(prompt, scriptType);
            _scriptCache[key] = script;
            return Task.CompletedTask;
        }

        public Task<bool> IsCachedAsync(string prompt, ScriptType scriptType)
        {
            var key = GenerateCacheKey(prompt, scriptType);
            return Task.FromResult(_scriptCache.ContainsKey(key));
        }

        public Task InvalidateCacheAsync(string prompt, ScriptType scriptType)
        {
            var key = GenerateCacheKey(prompt, scriptType);
            _scriptCache.Remove(key);
            return Task.CompletedTask;
        }
    }
}
