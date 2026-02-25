using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Sales;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class SaleEndpoints
{
    public static void MapSaleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sales").WithTags("Sales").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, int? customerId, int? salesmanId, SaleService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, customerId, salesmanId);
            return Results.Ok(ApiResponse<PagedResult<SaleDto>>.Ok(result));
        }).RequirePermission("Sales:View");

        group.MapGet("/{id:int}", async (int id, SaleService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<SaleDto>.Ok(result));
        }).RequirePermission("Sales:View");

        group.MapPost("/", async (CreateSaleRequest request, SaleService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/sales/{id}", ApiResponse<int>.Ok(id, "Sale created."));
        }).RequirePermission("Sales:Create");

        group.MapPut("/{id:int}/cancel", async (int id, SaleService service) =>
        {
            await service.CancelAsync(id);
            return Results.Ok(ApiResponse.Ok("Sale cancelled. Inventory restored."));
        }).RequirePermission("Sales:Edit");
    }
}
