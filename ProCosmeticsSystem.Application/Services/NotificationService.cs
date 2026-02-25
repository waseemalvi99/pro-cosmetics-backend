using ProCosmeticsSystem.Application.DTOs.Notifications;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly INotificationHubService _hubService;

    public NotificationService(INotificationRepository repo, INotificationHubService hubService)
    {
        _repo = repo;
        _hubService = hubService;
    }

    public async Task SendAsync(int userId, string title, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message
        };

        await _repo.CreateAsync(notification);
        await _hubService.SendToUserAsync(userId.ToString(), "ReceiveNotification", new NotificationDto
        {
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = notification.CreatedAt
        });
    }

    public Task<List<NotificationDto>> GetUserNotificationsAsync(int userId) =>
        _repo.GetByUserIdAsync(userId);

    public Task MarkAsReadAsync(int notificationId) =>
        _repo.MarkAsReadAsync(notificationId);
}
