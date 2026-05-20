namespace WMS.Domain.Interfaces;

public interface ICurrentUser
{
    Guid Id { get; }
    string Email { get; }
    string Role { get; }
    Dictionary<string, bool> Permissions { get; }
    bool IsAuthenticated { get; }
    string? Jti { get; }
    string? IpAddress { get; }
    TimeSpan TokenRemainingTime { get; }
}
