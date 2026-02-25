using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Sales;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class SalesmanEndpoints
{
    public static void MapSalesmanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/salesmen").WithTags("Salesmen").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, string? search, SalesmanService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, search);
            return Results.Ok(ApiResponse<PagedResult<SalesmanDto>>.Ok(result));
        }).RequirePermission("Sales:View");

        group.MapGet("/{id:int}", async (int id, SalesmanService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<SalesmanDto>.Ok(result));
        }).RequirePermission("Sales:View");

        group.MapPost("/", async (CreateSalesmanRequest request, SalesmanService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/salesmen/{id}", ApiResponse<int>.Ok(id, "Salesman created."));
        }).RequirePermission("Sales:Create");

        group.MapPut("/{id:int}", async (int id, UpdateSalesmanRequest request, SalesmanService service) =>
        {
            await service.UpdateAsync(id, request);
            return Results.Ok(ApiResponse.Ok("Salesman updated."));
        }).RequirePermission("Sales:Edit");

        group.MapDelete("/{id:int}", async (int id, SalesmanService service) =>
        {
            await service.DeleteAsync(id);
            return Results.Ok(ApiResponse.Ok("Salesman deleted."));
        }).RequirePermission("Sales:Delete");
    }
}
