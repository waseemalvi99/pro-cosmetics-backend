using Microsoft.AspNetCore.SignalR;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Infrastructure.Hubs;

namespace ProCosmeticsSystem.Infrastructure.Services;

public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendToUserAsync(string userId, string method, object message) =>
        _hubContext.Clients.Group($"user_{userId}").SendAsync(method, message);

    public Task SendToAllAsync(string method, object message) =>
        _hubContext.Clients.All.SendAsync(method, message);
}
