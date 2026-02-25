using Dapper;
using ProCosmeticsSystem.Application.DTOs.Notifications;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly DbConnectionFactory _db;

    public NotificationRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(Notification notification)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt)
              VALUES (@UserId, @Title, @Message, 0, @CreatedAt);
              SELECT CAST(SCOPE_IDENTITY() AS INT)",
            notification);
    }

    public async Task<List<NotificationDto>> GetByUserIdAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<NotificationDto>(
            "SELECT Id, Title, Message, IsRead, CreatedAt FROM Notifications WHERE UserId = @UserId ORDER BY CreatedAt DESC",
            new { UserId = userId });
        return results.ToList();
    }

    public async Task MarkAsReadAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Notifications SET IsRead = 1 WHERE Id = @Id", new { Id = id });
    }
}
