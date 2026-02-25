using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Deliveries;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class DeliveryEndpoints
{
    public static void MapDeliveryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/deliveries").WithTags("Deliveries").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, int? deliveryManId, string? status, DeliveryService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, deliveryManId, status);
            return Results.Ok(ApiResponse<PagedResult<DeliveryDto>>.Ok(result));
        }).RequirePermission("Deliveries:View");

        group.MapGet("/{id:int}", async (int id, DeliveryService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<DeliveryDto>.Ok(result));
        }).RequirePermission("Deliveries:View");

        group.MapPost("/", async (CreateDeliveryRequest request, DeliveryService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/deliveries/{id}", ApiResponse<int>.Ok(id, "Delivery created."));
        }).RequirePermission("Deliveries:Create");

        group.MapPut("/{id:int}/pickup", async (int id, UpdateDeliveryStatusRequest request, DeliveryService service) =>
        {
            await service.PickupAsync(id, request);
            return Results.Ok(ApiResponse.Ok("Delivery picked up."));
        }).RequirePermission("Deliveries:Edit");

        group.MapPut("/{id:int}/deliver", async (int id, UpdateDeliveryStatusRequest request, DeliveryService service) =>
        {
            await service.DeliverAsync(id, request);
            return Results.Ok(ApiResponse.Ok("Delivery completed."));
        }).RequirePermission("Deliveries:Edit");
    }
}
