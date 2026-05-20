namespace WMS.Application.Auth.Interfaces;

public interface ITokenRevocationStore
{
    /// <summary>Revoke 1 token (lưu jti vào blocklist).</summary>
    Task RevokeAsync(string jti, TimeSpan ttl);

    /// <summary>Kiểm tra token đã bị revoke chưa.</summary>
    Task<bool> IsRevokedAsync(string jti);
}