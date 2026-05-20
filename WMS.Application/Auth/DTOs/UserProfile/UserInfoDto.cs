namespace WMS.Application.Auth.DTOs.UserProfile;

public record UserInfoDto(Guid Id, string Email, string FullName, string Role);