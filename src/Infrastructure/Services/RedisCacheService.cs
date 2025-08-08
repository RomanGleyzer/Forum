using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Services;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _ = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = redis.GetDatabase();
    }

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
            if (value.IsNullOrEmpty) return default;

            var bytes = (byte[]?)value;
            if (bytes is null || bytes.Length == 0) return default;

            return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving key '{Key}' from Redis for type {Type}.", key, typeof(T).Name);
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

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

            await _database.StringSetAsync(key, bytes, expiration).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key '{Key}' in Redis for type {Type}.", key, typeof(T).Name);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to remove a value with an empty Redis key.");
            return;
        }

        try
        {
            await _database.KeyDeleteAsync(key, CommandFlags.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key '{Key}' from Redis.", key);
        }
    }
}
