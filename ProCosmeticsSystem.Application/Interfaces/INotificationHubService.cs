namespace ProCosmeticsSystem.Application.Interfaces;

public interface INotificationHubService
{
    Task SendToUserAsync(string userId, string method, object message);
    Task SendToAllAsync(string method, object message);
}
