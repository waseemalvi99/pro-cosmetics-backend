using ProCosmeticsSystem.Application.DTOs.Combos;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class ComboEndpoints
{
    public static void MapComboEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/combos").WithTags("Combos").RequireAuthorization();

        group.MapGet("/customers", async (string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchCustomersAsync(search, limit ?? 20);
            return Results.Ok(ApiResponse<List<CustomerComboDto>>.Ok(result));
        });

        group.MapGet("/suppliers", async (string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchSuppliersAsync(search, limit ?? 20);
            return Results.Ok(ApiResponse<List<SupplierComboDto>>.Ok(result));
        });

        group.MapGet("/products", async (string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchProductsAsync(search, limit ?? 20);
            return Results.Ok(ApiResponse<List<ProductComboDto>>.Ok(result));
        });

        group.MapGet("/salesmen", async (string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchSalesmenAsync(search, limit ?? 20);
            return Results.Ok(ApiResponse<List<ComboItemDto>>.Ok(result));
        });

        group.MapGet("/categories", async (string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchCategoriesAsync(search, limit ?? 20);
            return Results.Ok(ApiResponse<List<ComboItemDto>>.Ok(result));
        });

        group.MapGet("/delivery-men", async (string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchDeliveryMenAsync(search, limit ?? 20);
            return Results.Ok(ApiResponse<List<DeliveryManComboDto>>.Ok(result));
        });

        group.MapGet("/users", async (string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchUsersAsync(search, limit ?? 20);
            return Results.Ok(ApiResponse<List<ComboItemDto>>.Ok(result));
        });

        group.MapGet("/roles", async (string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchRolesAsync(search, limit ?? 20);
            return Results.Ok(ApiResponse<List<ComboItemDto>>.Ok(result));
        });

        group.MapGet("/sales", async (int customerId, string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchSalesAsync(customerId, search, limit ?? 20);
            return Results.Ok(ApiResponse<List<SaleComboDto>>.Ok(result));
        });

        group.MapGet("/purchase-orders", async (int supplierId, string? search, int? limit, ComboService service) =>
        {
            var result = await service.SearchPurchaseOrdersAsync(supplierId, search, limit ?? 20);
            return Results.Ok(ApiResponse<List<PurchaseOrderComboDto>>.Ok(result));
        });
    }
}
