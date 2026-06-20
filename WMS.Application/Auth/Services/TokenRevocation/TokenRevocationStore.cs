using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WMS.Application.Auth.Interfaces;

namespace WMS.Application.Auth.Services.TokenRevocation;

public class TokenRevocationStore(
    IConnectionMultiplexer? redis,
    ILogger<TokenRevocationStore> log) : ITokenRevocationStore
{
    private readonly IConnectionMultiplexer? _redis = redis;
    private readonly ILogger<TokenRevocationStore> _log = log;

    private const string KeyPrefix = "revoked_token:";

    public async Task RevokeAsync(string jti, TimeSpan ttl)
    {
        if (_redis is null || !_redis.IsConnected)
        {
            _log.LogWarning("Redis not available — token revocation skipped for jti={Jti}", jti);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}{jti}";
            await db.StringSetAsync(key, "1", ttl);

            _log.LogInformation("Token revoked: jti={Jti} ttl={Ttl}", jti, ttl);
        }
        catch (RedisConnectionException ex)
        {
            _log.LogWarning(ex, "Redis connection failed — token revocation skipped for jti={Jti}", jti);
        }
    }

    public async Task<bool> IsRevokedAsync(string jti)
    {
        if (_redis is null || !_redis.IsConnected) return false;

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{KeyPrefix}{jti}";
            return await db.KeyExistsAsync(key);
        }
        catch (RedisConnectionException ex)
        {
            _log.LogWarning(ex,
                "Redis connection failed — treating token as not revoked for jti={Jti}", jti);
            return false;
        }
    }
}
