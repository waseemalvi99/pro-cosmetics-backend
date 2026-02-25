using ProCosmeticsSystem.Application.DTOs.Notifications;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface INotificationRepository
{
    Task<int> CreateAsync(Notification notification);
    Task<List<NotificationDto>> GetByUserIdAsync(int userId);
    Task MarkAsReadAsync(int id);
}
