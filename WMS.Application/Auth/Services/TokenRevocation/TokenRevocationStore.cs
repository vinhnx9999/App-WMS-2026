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
        if (_redis is null)
        {
            _log.LogWarning("Redis not available — token revocation skipped");
            return;
        }

        var db = _redis.GetDatabase();
        var key = $"{KeyPrefix}{jti}";
        await db.StringSetAsync(key, "1", ttl);

        _log.LogInformation("Token revoked: jti={Jti} ttl={Ttl}", jti, ttl);
    }

    public async Task<bool> IsRevokedAsync(string jti)
    {
        if (_redis is null) return false;

        var db = _redis.GetDatabase();
        var key = $"{KeyPrefix}{jti}";
        return await db.KeyExistsAsync(key);
    }
}
