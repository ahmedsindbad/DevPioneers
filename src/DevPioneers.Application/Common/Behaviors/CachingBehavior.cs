// ============================================
// File: DevPioneers.Application/Common/Behaviors/CachingBehavior.cs
// ============================================
using MediatR;
using Microsoft.Extensions.Logging;
using DevPioneers.Application.Common.Interfaces;
using System.Text.Json;

namespace DevPioneers.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for caching query results
/// Only applies to queries that implement ICacheableQuery
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class // تم إضافة constraint للـ reference types فقط
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
            return await next();
        }

        var cacheKey = GenerateCacheKey(request, cacheableQuery);

        try
        {
            // Try to get from cache
            var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
            if (cachedResponse != null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return cachedResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving from cache for {CacheKey}", cacheKey);
            // Continue without cache
        }

        // Get from handler
        var response = await next();

        try
        {
            // Cache the response
            var cacheExpiry = cacheableQuery.GetCacheExpiration() ?? TimeSpan.FromMinutes(5);
            await _cacheService.SetAsync(cacheKey, response, cacheExpiry, cancellationToken);
            _logger.LogDebug("Cached response for {CacheKey} with expiry {CacheExpiry}", cacheKey, cacheExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error caching response for {CacheKey}", cacheKey);
            // Continue without caching
        }

        return response;
    }

    private string GenerateCacheKey(TRequest request, ICacheableQuery cacheableQuery)
    {
        var customKey = cacheableQuery.GetCacheKey();
        if (!string.IsNullOrEmpty(customKey))
        {
            return customKey;
        }

        // Auto-generate key from request type and properties
        var requestTypeName = typeof(TRequest).Name;
        try
        {
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var hash = GetStringHash(requestJson);
            return $"{requestTypeName}:{hash}";
        }
        catch
        {
            return $"{requestTypeName}:default";
        }
    }

    private string GetStringHash(string input)
    {
        var hash = 0;
        foreach (char c in input)
        {
            hash = ((hash << 5) - hash) + c;
            hash = hash & hash;
        }
        return Math.Abs(hash).ToString();
    }
}

/// <summary>
/// Interface to mark queries as cacheable
/// </summary>
public interface ICacheableQuery
{
    string? GetCacheKey() => null;
    TimeSpan? GetCacheExpiration() => TimeSpan.FromMinutes(5);
}