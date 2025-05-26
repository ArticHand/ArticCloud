# Azure Infrastructure Generator

## Overview
This .NET Core application helps generate Azure infrastructure deployment scripts using AI-powered natural language processing.

## Features
- Generate infrastructure scripts from user prompts
- Support for Bicep, PowerShell, and Terraform
- OpenAI-powered script generation

## Prerequisites
- .NET 8.0 SDK
- OpenAI API Key

## Configuration
1. Set your OpenAI API key in `appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "your_openai_api_key_here"
  },
  "Redis": {
    "ConnectionString": "your_redis_connection_string_here"
  }
}
```

### Redis Caching
- Install Redis server locally or use cloud-hosted Redis
- Recommended: Azure Cache for Redis or Redis Enterprise
- Connection string format: `host:port,password=yourpassword`

## Caching Strategies
The application supports multiple caching strategies:
- In-Memory Cache (Default)
- Distributed Redis Cache
- Custom Cache Implementations

### Configuring Redis Cache
```csharp
services.AddRedisCache(Configuration["Redis:ConnectionString"]);
```

### Cache Invalidation
Multiple strategies for cache management:

#### Invalidate Specific Key
```csharp
// Invalidate a specific cache entry
await _cacheInvalidationService.InvalidateCacheAsync(cacheKey, scriptType);
```

#### Invalidate by Script Type
```csharp
// Remove all caches for Bicep scripts
await _cacheInvalidationService.InvalidateByScriptTypeAsync(ScriptType.Bicep);
```

#### Invalidate by Age
```csharp
// Remove caches older than 7 days
await _cacheInvalidationService.InvalidateByAgeAsync(TimeSpan.FromDays(7));
```

#### Custom Invalidation
```csharp
// Remove caches with low hit count
await _cacheInvalidationService.InvalidateByPredicateAsync(
    metadata => metadata.HitCount < 3
);
```

## Usage
Run the CLI application and enter your cloud deployment needs:
```
dotnet run
Enter your cloud deployment needs: I need an API hosted in Azure
```

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License
[MIT](https://choosealicense.com/licenses/mit/)
