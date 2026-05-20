using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace WMS.Application.SignalR;

public class DashboardHub(ILogger<DashboardHub> log) : Hub
{
    private readonly ILogger<DashboardHub> _log = log;

    // Track online users: ConnectionId → UserId
    private static readonly ConcurrentDictionary<string, string> _connections = new();

    public static int ConnectedCount => _connections.Count;

    // ═══ Lifecycle ═══

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier
            ?? Context.User?.FindFirst("nameid")?.Value
            ?? "anonymous";

        _connections[Context.ConnectionId] = userId;

        // Auto-join "dashboard" group — nhận tất cả events
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");

        _log.LogInformation(
            "Dashboard client connected: {ConnId} user={User} total={Count}",
            Context.ConnectionId, userId, ConnectedCount);

        // Gửi summary ngay khi connect
        await Clients.Caller.SendAsync("connected", new
        {
            connectionId = Context.ConnectionId,
            connectedAt = DateTime.UtcNow,
            connectedClients = ConnectedCount,
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        _connections.TryRemove(Context.ConnectionId, out var _);

        // Rời tất cả zone groups
        foreach (var zoneGroup in GetZoneGroups())
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, zoneGroup);

        _log.LogInformation(
            "Dashboard client disconnected: {ConnId} remaining={Count}",
            Context.ConnectionId, ConnectedCount);

        await base.OnDisconnectedAsync(ex);
    }

    // ═══ Client-invokable methods ═══

    /// <summary>
    /// Join zone-specific group — nhận events cho zone đó.
    /// Usage: await connection.invoke("JoinZone", "zone-guid");
    /// </summary>
    public async Task JoinZone(string zoneId)
    {
        var group = $"zone:{zoneId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        _log.LogDebug("{ConnId} joined {Group}", Context.ConnectionId, group);
    }

    /// <summary>
    /// Leave zone group.
    /// </summary>
    public async Task LeaveZone(string zoneId)
    {
        var group = $"zone:{zoneId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        _log.LogDebug("{ConnId} left {Group}", Context.ConnectionId, group);
    }

    /// <summary>
    /// Ping — client dùng để check connection alive.
    /// </summary>
    public Task<string> Ping() => Task.FromResult("pong");

    // ═══ Helpers ═══

    private static IEnumerable<string> GetZoneGroups()
    {
        // Simplified — in production, track per-connection group memberships
        yield break;
    }
}
