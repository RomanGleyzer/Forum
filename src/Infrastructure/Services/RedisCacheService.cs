using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services;

public sealed class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ILogger<RedisCacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to get a value with an empty Redis key.");
            return default;
        }

        try
        {
            var value = await _database.StringGetAsync(key).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (value.IsNullOrEmpty) return default;

            try
            {
                var bytes = (byte[]?)value;
                if (bytes is null || bytes.Length == 0) return default;

                return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
            }
            catch (JsonException jex)
            {
                _logger.LogWarning(jex,
                    "Corrupted JSON for key '{Key}' in Redis. Deleting the key to self-heal. Target type: {Type}.",
                    key, typeof(T).Name);

                try
                {
                    await _database.KeyDeleteAsync(key, CommandFlags.None).ConfigureAwait(false);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogDebug(cleanupEx, "Failed to delete corrupted key '{Key}' from Redis.", key);
                }

                return default;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RedisException rex)
        {
            _logger.LogError(rex, "Redis error while getting key '{Key}' for type {Type}.", key, typeof(T).Name);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving key '{Key}' from Redis for type {Type}.", key, typeof(T).Name);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to set a value with an empty Redis key.");
            return;
        }

        if (expiration.HasValue && expiration.Value < TimeSpan.Zero)
        {
            _logger.LogWarning("Negative expiration provided for key '{Key}'. Ignoring TTL.", key);
            expiration = null;
        }

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

            await _database.StringSetAsync(key, bytes, expiration).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "JSON serialization error when setting key '{Key}' for type {Type}.", key, typeof(T).Name);
        }
        catch (RedisException rex)
        {
            _logger.LogError(rex, "Redis error while setting key '{Key}' for type {Type}.", key, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting key '{Key}' in Redis for type {Type}.", key, typeof(T).Name);
        }
    }
}
