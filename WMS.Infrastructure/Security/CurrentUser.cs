using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using WMS.Domain.Interfaces;

namespace WMS.Infrastructure.Security;

public class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private readonly IHttpContextAccessor _http = http;

    public Guid Id
    {
        get
        {
            var claim = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }

    public string Email =>
        _http.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value ?? "";

    public string Role =>
        _http.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    public Dictionary<string, bool> Permissions =>
        _http.HttpContext?.User.FindAll("permission")
            .ToDictionary(c => c.Value, c => true) ?? [];

    public bool IsAuthenticated =>
        _http.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    // NEW: JWT ID claim
    public string? Jti =>
        _http.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

    // NEW: Client IP
    public string? IpAddress =>
        _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    // NEW: Tính thời gian còn lại của token
    public TimeSpan TokenRemainingTime
    {
        get
        {
            var expClaim = _http.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
            if (expClaim is null) return TimeSpan.Zero;

            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
            var remaining = exp - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
