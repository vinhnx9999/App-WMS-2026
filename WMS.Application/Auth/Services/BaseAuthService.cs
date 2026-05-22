using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WMS.Domain.Common;
using WMS.Domain.Entities.Security;
using Role = WMS.Domain.Entities.Security.Role;

namespace WMS.Application.Auth.Services;

public class BaseAuthService(IConfiguration config)
{
    private readonly IConfiguration _config = config;

    // Google OIDC discovery document URL
    protected const string GoogleIssuer = "https://accounts.google.com";
    protected const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";
    //protected const string GoogleUserInfoEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
    protected const string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

    protected const string GraphApiBase = "https://graph.facebook.com/v19.0";
    protected const string FbAuthEndpoint = "https://www.facebook.com/v19.0/dialog/oauth";
    protected const string FbTokenEndpoint = "https://graph.facebook.com/v19.0/oauth/access_token";

    protected const string XAuthEndpoint = "https://twitter.com/i/oauth2/authorize";
    protected const string XTokenEndpoint = "https://api.twitter.com/2/oauth2/token";
    protected const string XUserEndpoint = "https://api.twitter.com/2/users/me";

    // Redis key prefix cho PKCE state
    protected const string PkcePrefix = "x_pkce:";

    // Microsoft Identity Platform endpoints
    // {tenant} = "common" | "consumers" | "organizations" | "{tenant-id}"
    protected string Tenant => _config["Microsoft:Tenant"] ?? "common";
    protected const string MsAuthTemplate =
        "https://login.microsoftonline.com/{0}/oauth2/v2.0/authorize";
    protected const string MsTokenTemplate =
        "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";
    protected const string MsOidcTemplate =
        "https://login.microsoftonline.com/{0}/v2.0/.well-known/openid-configuration";

    // LinkedIn OAuth 2.0 endpoints
    protected const string LiAuthEndpoint = "https://www.linkedin.com/oauth/v2/authorization";
    protected const string LiTokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken";
    protected const string LiUserinfoEndpoint = "https://api.linkedin.com/v2/userinfo";

    protected string GenerateAccessToken(User user, Role role, string jti = "")
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));

        if (string.IsNullOrEmpty(jti)) jti = $"{Guid.NewGuid()}";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),                  // Token ID
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role.Name),
            new(ClaimTypes.Version, SystemDefine.SystemVersion),
            new(ClaimTypes.System, SystemDefine.AppWMS),
            new("tenant_id", user.TenantId.ToString()),
            new("auth_provider", user.AuthProvider),
        };

        foreach (var p in role.Permissions)
            claims.Add(new Claim("permission", p.Key));

        var expiry = double.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: new(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>SHA256(verifier) → base64url</summary>
    protected static string GenerateCodeChallenge(string verifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>Random 32-byte string, base64url encoded</summary>
    protected static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>Random state for CSRF protection</summary>
    protected static string GenerateState()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
