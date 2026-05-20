namespace WMS.Application.SignalR.DTOs;

public record DashboardEvent<T>
{
    public string Event { get; init; } = "";
    public T Data { get; init; } = default!;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
