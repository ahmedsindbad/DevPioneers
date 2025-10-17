// ============================================
// File: DevPioneers.Application/Common/Behaviors/CachingBehavior.cs (Fixed)
// ============================================
using DevPioneers.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DevPioneers.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for caching query responses
/// Only works with IRequest<TResponse> where TResponse is a class (reference type)
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class // Add class constraint to ensure TResponse is a reference type
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheService cacheService,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        // Only cache if request implements ICacheableQuery
        if (request is not ICacheableQuery cacheableQuery)
        {
            // Not cacheable, proceed normally
            return await next();
        }

        // Generate cache key
        var cacheKey = GenerateCacheKey(request, cacheableQuery);

        // Try to get from cache
        try
        {
            var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
                return cachedResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve from cache for key: {CacheKey}", cacheKey);
        }

        // Cache miss - execute request
        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
        var response = await next();

        // Cache the response if not null
        if (response != null)
        {
            try
            {
                var expiration = cacheableQuery.GetCacheExpiration();
                await _cacheService.SetAsync(cacheKey, response, expiration, cancellationToken);
                _logger.LogDebug("Cached response for key: {CacheKey} with expiration: {Expiration}", 
                    cacheKey, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache response for key: {CacheKey}", cacheKey);
            }
        }

        return response;
    }

    /// <summary>
    /// Generate cache key based on request type and properties
    /// </summary>
    private string GenerateCacheKey(TRequest request, ICacheableQuery cacheableQuery)
    {
        var requestTypeName = typeof(TRequest).Name;
        var customKey = cacheableQuery.GetCacheKey();

        if (!string.IsNullOrEmpty(customKey))
        {
            return $"{requestTypeName}:{customKey}";
        }

        // Generate key from request properties
        try
        {
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Create a hash of the JSON for consistent key generation
            var hash = GetStringHash(requestJson);
            return $"{requestTypeName}:{hash}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate cache key from request properties for {RequestType}", requestTypeName);
            // Fallback to simple key
            return $"{requestTypeName}:fallback";
        }
    }

    /// <summary>
    /// Generate consistent hash from string
    /// </summary>
    private string GetStringHash(string input)
    {
        var hash = 0;
        foreach (char c in input)
        {
            hash = ((hash << 5) - hash) + c;
            hash = hash & hash; // Convert to 32-bit integer
        }
        return Math.Abs(hash).ToString();
    }
}

/// <summary>
/// Interface to mark queries as cacheable
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Get cache key for the query (optional - can return null for auto-generation)
    /// </summary>
    string? GetCacheKey() => null;

    /// <summary>
    /// Get cache expiration time
    /// </summary>
    TimeSpan? GetCacheExpiration() => TimeSpan.FromMinutes(5); // Default 5 minutes
}

/// <summary>
/// Alternative caching behavior for value types (if needed)
/// This version doesn't use caching but logs for debugging
/// </summary>
public class ValueTypeCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : struct // For value types
{
    private readonly ILogger<ValueTypeCachingBehavior<TRequest, TResponse>> _logger;

    public ValueTypeCachingBehavior(ILogger<ValueTypeCachingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        // Value types are not cached in this implementation
        // You could implement specialized caching here if needed
        _logger.LogDebug("Processing value type request: {RequestType}", typeof(TRequest).Name);
        return await next();
    }
}
