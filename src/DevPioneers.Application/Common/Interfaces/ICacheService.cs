// ============================================
// File: DevPioneers.Application/Common/Interfaces/ICacheService.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Cache service interface for Redis operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cached value by key
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Get cached string value by key
    /// </summary>
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set cached value with expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Set cached string value with expiration
    /// </summary>
    Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove cached value by key
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove multiple cached values by pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or set cached value (cache-aside pattern)
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> getItem,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Increment numeric value in cache
    /// </summary>
    Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set expiration for existing key
    /// </summary>
    Task<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);
}