using System.Text.Json.Serialization;

namespace WMS.Application.Auth.DTOs.Facebook;

/// <summary>
/// Facebook user info lấy từ Graph API /me.
/// </summary>
public record FacebookUserInfo
{
    public string FacebookId { get; init; } = "";
    public string Email { get; init; } = "";        // Có thể rỗng
    public string Name { get; init; } = "";
    public string? PictureUrl { get; init; }
    public bool EmailVerified { get; init; }

    /// <summary>Helper: tạo email giả nếu Facebook không trả email</summary>
    public string EffectiveEmail =>
        !string.IsNullOrEmpty(Email)
            ? Email
            : $"fb_{FacebookId}@facebook.wms.local";
}