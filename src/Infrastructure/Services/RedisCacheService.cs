using Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Services;

public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly ILogger<RedisCacheService> _logger = logger;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue) return default;

            var valueString = value.ToString();
            if (string.IsNullOrEmpty(valueString)) return default;

            return JsonSerializer.Deserialize<T>(valueString, _serializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving key {Key} from Redis", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var valueString = JsonSerializer.Serialize(value, _serializerOptions);
            var redisValue = new RedisValue(valueString);

            await _db.StringSetAsync(key, redisValue, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} in Redis", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key, CommandFlags.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from Redis", key);
        }
    }
}
