using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Purchases;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class PurchaseOrderEndpoints
{
    public static void MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/purchase-orders").WithTags("Purchase Orders").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, int? supplierId, PurchaseOrderService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, supplierId);
            return Results.Ok(ApiResponse<PagedResult<PurchaseOrderDto>>.Ok(result));
        }).RequirePermission("Purchases:View");

        group.MapGet("/{id:int}", async (int id, PurchaseOrderService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
        }).RequirePermission("Purchases:View");

        group.MapPost("/", async (CreatePurchaseOrderRequest request, PurchaseOrderService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/purchase-orders/{id}", ApiResponse<int>.Ok(id, "Purchase order created."));
        }).RequirePermission("Purchases:Create");

        group.MapPut("/{id:int}/submit", async (int id, PurchaseOrderService service) =>
        {
            await service.SubmitAsync(id);
            return Results.Ok(ApiResponse.Ok("Purchase order submitted."));
        }).RequirePermission("Purchases:Edit");

        group.MapPut("/{id:int}/receive", async (int id, ReceivePurchaseOrderRequest request, PurchaseOrderService service) =>
        {
            await service.ReceiveAsync(id, request);
            return Results.Ok(ApiResponse.Ok("Purchase order received. Inventory updated."));
        }).RequirePermission("Purchases:Edit");

        group.MapPut("/{id:int}/cancel", async (int id, PurchaseOrderService service) =>
        {
            await service.CancelAsync(id);
            return Results.Ok(ApiResponse.Ok("Purchase order cancelled."));
        }).RequirePermission("Purchases:Edit");
    }
}
