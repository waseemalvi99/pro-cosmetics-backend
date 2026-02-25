using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Suppliers;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/suppliers").WithTags("Suppliers").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, string? search, SupplierService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, search);
            return Results.Ok(ApiResponse<PagedResult<SupplierDto>>.Ok(result));
        }).RequirePermission("Suppliers:View");

        group.MapGet("/{id:int}", async (int id, SupplierService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<SupplierDto>.Ok(result));
        }).RequirePermission("Suppliers:View");

        group.MapPost("/", async (CreateSupplierRequest request, SupplierService service) =>
        {
            var id = await service.CreateAsync(request);
            return Results.Created($"/api/suppliers/{id}", ApiResponse<int>.Ok(id, "Supplier created successfully."));
        }).RequirePermission("Suppliers:Create");

        group.MapPut("/{id:int}", async (int id, UpdateSupplierRequest request, SupplierService service) =>
        {
            await service.UpdateAsync(id, request);
            return Results.Ok(ApiResponse.Ok("Supplier updated successfully."));
        }).RequirePermission("Suppliers:Edit");

        group.MapDelete("/{id:int}", async (int id, SupplierService service) =>
        {
            await service.DeleteAsync(id);
            return Results.Ok(ApiResponse.Ok("Supplier deleted successfully."));
        }).RequirePermission("Suppliers:Delete");
    }
}
