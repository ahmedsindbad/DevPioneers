// ============================================
// File: DevPioneers.Infrastructure/Services/Cache/RedisService.cs
// ============================================
using System.Text.Json;
using DevPioneers.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DevPioneers.Infrastructure.Services.Cache;

/// <summary>
/// Redis cache service implementation
/// </summary>
public class RedisService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisService(
        IConnectionMultiplexer redis,
        ILogger<RedisService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _redis.GetDatabase();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var value = await _database.StringGetAsync(key);

            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);

            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var value = await _database.StringGetAsync(key);

            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached string for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);

            var success = await _database.StringSetAsync(
                key,
                serializedValue,
                expiration);

            if (success)
            {
                _logger.LogDebug("Cached value set for key: {Key} with expiration: {Expiration}",
                    key, expiration?.ToString() ?? "None");
            }
            else
            {
                _logger.LogWarning("Failed to set cache value for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));

        try
        {
            var success = await _database.StringSetAsync(
                key,
                value,
                expiration);

            if (success)
            {
                _logger.LogDebug("Cached string set for key: {Key} with expiration: {Expiration}",
                    key, expiration?.ToString() ?? "None");
            }
            else
            {
                _logger.LogWarning("Failed to set cache string for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached string for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var success = await _database.KeyDeleteAsync(key);

            if (success)
            {
                _logger.LogDebug("Cache key removed: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache key not found for removal: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentNullException(nameof(pattern));

        try
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            var keys = server.Keys(pattern: pattern).ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}",
                    keys.Length, pattern);
            }
            else
            {
                _logger.LogDebug("No cache keys found matching pattern: {Pattern}", pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> getItem,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        if (getItem == null)
            throw new ArgumentNullException(nameof(getItem));

        try
        {
            // Try to get from cache first
            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            // Cache miss - get the item and cache it
            _logger.LogDebug("Cache miss for key: {Key}, fetching from source", key);
            var item = await getItem(cancellationToken);

            if (item != null)
            {
                await SetAsync(key, item, expiration, cancellationToken);
            }

            return item!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var result = await _database.StringIncrementAsync(key, value);
            _logger.LogDebug("Incremented cache key: {Key} by {Value}, new value: {Result}",
                key, value, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing cache key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        try
        {
            var success = await _database.KeyExpireAsync(key, expiration);

            if (success)
            {
                _logger.LogDebug("Set expiration for key: {Key} to {Expiration}",
                    key, expiration);
            }
            else
            {
                _logger.LogWarning("Failed to set expiration for key: {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting expiration for key: {Key}", key);
            return false;
        }
    }
}
