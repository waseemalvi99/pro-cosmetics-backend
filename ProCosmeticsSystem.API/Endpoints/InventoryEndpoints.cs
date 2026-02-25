using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Products;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory").WithTags("Inventory").RequireAuthorization();

        group.MapGet("/", async (InventoryService service) =>
        {
            var result = await service.GetAllAsync();
            return Results.Ok(ApiResponse<List<InventoryDto>>.Ok(result));
        }).RequirePermission("Products:View");

        group.MapGet("/low-stock", async (InventoryService service) =>
        {
            var result = await service.GetLowStockAsync();
            return Results.Ok(ApiResponse<List<InventoryDto>>.Ok(result));
        }).RequirePermission("Products:View");

        group.MapPost("/adjust", async (AdjustInventoryRequest request, InventoryService service) =>
        {
            await service.AdjustAsync(request);
            return Results.Ok(ApiResponse.Ok("Inventory adjusted successfully."));
        }).RequirePermission("Products:Edit");
    }
}
