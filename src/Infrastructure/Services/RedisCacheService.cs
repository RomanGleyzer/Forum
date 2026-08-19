using Application.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to get a value with an empty Redis key.");
            return default;
        }

        try
        {
            if (cancellationToken.IsCancellationRequested) return default;
            var value = await _database
                .StringGetAsync(key)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

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

                _ = _database.KeyDeleteAsync(key, CommandFlags.FireAndForget);
                return default;
            }
            catch (NotSupportedException nsex)
            {
                _logger.LogWarning(nsex,
                    "Type not supported for JSON deserialization for key '{Key}'. Target type: {Type}.",
                    key, typeof(T));
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
            _logger.LogError(ex, "Unexpected error retrieving key '{Key}' from Redis for type {Type}.", key,
                typeof(T).Name);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to set a value with an empty Redis key.");
            return;
        }

        if (expiration is { } e && e <= TimeSpan.Zero)
        {
            _logger.LogWarning("Non-positive expiration provided for key '{Key}'. Ignoring TTL.", key);
            expiration = null;
        }

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

            await _database
                .StringSetAsync(key, bytes, expiration)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "JSON serialization error when setting key '{Key}' for type {Type}.", key,
                typeof(T).Name);
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