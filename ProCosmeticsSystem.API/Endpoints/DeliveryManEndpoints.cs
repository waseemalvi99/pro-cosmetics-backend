using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Deliveries;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.API.Endpoints;

public static class DeliveryManEndpoints
{
    public static void MapDeliveryManEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-men").WithTags("Delivery Men").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, string? search, IDeliveryManRepository repo) =>
        {
            page = page < 1 ? 1 : page ?? 1;
            pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize ?? 20;
            var result = await repo.GetAllAsync(page.Value, pageSize.Value, search);
            return Results.Ok(ApiResponse<PagedResult<DeliveryManDto>>.Ok(result));
        }).RequirePermission("Deliveries:View");

        group.MapGet("/{id:int}", async (int id, IDeliveryManRepository repo) =>
        {
            var result = await repo.GetByIdAsync(id) ?? throw new NotFoundException("DeliveryMan", id);
            return Results.Ok(ApiResponse<DeliveryManDto>.Ok(result));
        }).RequirePermission("Deliveries:View");

        group.MapPost("/", async (CreateDeliveryManRequest request, IDeliveryManRepository repo, ICurrentUserService currentUser) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Name", "Name is required.");

            var dm = new DeliveryMan
            {
                Name = request.Name,
                Phone = request.Phone,
                Email = request.Email,
                CreatedBy = currentUser.UserId
            };
            var id = await repo.CreateAsync(dm);
            return Results.Created($"/api/delivery-men/{id}", ApiResponse<int>.Ok(id, "Delivery man created."));
        }).RequirePermission("Deliveries:Create");

        group.MapPut("/{id:int}", async (int id, UpdateDeliveryManRequest request, IDeliveryManRepository repo, ICurrentUserService currentUser) =>
        {
            _ = await repo.GetByIdAsync(id) ?? throw new NotFoundException("DeliveryMan", id);
            await repo.UpdateAsync(new DeliveryMan
            {
                Id = id,
                Name = request.Name,
                Phone = request.Phone,
                Email = request.Email,
                IsAvailable = request.IsAvailable,
                IsActive = request.IsActive,
                UpdatedBy = currentUser.UserId,
                UpdatedAt = DateTime.UtcNow
            });
            return Results.Ok(ApiResponse.Ok("Delivery man updated."));
        }).RequirePermission("Deliveries:Edit");

        group.MapDelete("/{id:int}", async (int id, IDeliveryManRepository repo) =>
        {
            _ = await repo.GetByIdAsync(id) ?? throw new NotFoundException("DeliveryMan", id);
            await repo.SoftDeleteAsync(id);
            return Results.Ok(ApiResponse.Ok("Delivery man deleted."));
        }).RequirePermission("Deliveries:Delete");
    }
}
