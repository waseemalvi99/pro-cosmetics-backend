using ProCosmeticsSystem.Application.DTOs.Notifications;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface INotificationService
{
    Task SendAsync(int userId, string title, string message);
    Task<List<NotificationDto>> GetUserNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
}
