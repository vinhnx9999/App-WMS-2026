namespace WMS.Application.Auth.DTOs.Logout;

public record LogoutRequest
{
    /// <summary>Refresh token cần revoke. Bắt buộc.</summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Revoke tất cả refresh tokens của user (logout all devices).
    /// </summary>
    public bool LogoutAllDevices { get; init; }
}