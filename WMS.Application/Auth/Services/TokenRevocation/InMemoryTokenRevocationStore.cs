using System.Collections.Concurrent;
using WMS.Application.Auth.Interfaces;

namespace WMS.Application.Auth.Services.TokenRevocation;

/// <summary>
/// In-memory fallback when Redis is unavailable.
/// WARNING: Not shared across instances. Only for single-server dev setup.
/// </summary>
public class InMemoryTokenRevocationStore : ITokenRevocationStore
{
    private static readonly ConcurrentDictionary<string, DateTime> _revoked = new();

    public Task RevokeAsync(string jti, TimeSpan ttl)
    {
        _revoked[jti] = DateTime.UtcNow.Add(ttl);

        // Cleanup expired entries periodically
        if (_revoked.Count > 1000)
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _revoked)
                if (kvp.Value < now) _revoked.TryRemove(kvp.Key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(string jti)
    {
        if (_revoked.TryGetValue(jti, out var expires))
        {
            if (expires > DateTime.UtcNow)
                return Task.FromResult(true);

            _revoked.TryRemove(jti, out _);
        }

        return Task.FromResult(false);
    }
}