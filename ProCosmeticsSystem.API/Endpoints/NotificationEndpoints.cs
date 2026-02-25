using ProCosmeticsSystem.API.Extensions;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Notifications;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.API.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async (HttpContext context, INotificationService notificationService) =>
        {
            var userId = context.User.GetUserId();
            var result = await notificationService.GetUserNotificationsAsync(userId);
            return Results.Ok(ApiResponse<List<NotificationDto>>.Ok(result));
        });

        group.MapPut("/{id:int}/read", async (int id, INotificationService notificationService) =>
        {
            await notificationService.MarkAsReadAsync(id);
            return Results.Ok(ApiResponse.Ok("Notification marked as read."));
        });
    }
}
