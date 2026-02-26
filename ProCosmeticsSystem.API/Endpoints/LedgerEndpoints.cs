using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Ledger;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class LedgerEndpoints
{
    public static void MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ledger").WithTags("Ledger").RequireAuthorization();

        group.MapGet("/", async (int? page, int? pageSize, int? customerId, int? supplierId, LedgerService service) =>
        {
            var result = await service.GetAllAsync(page ?? 1, pageSize ?? 20, customerId, supplierId);
            return Results.Ok(ApiResponse<PagedResult<LedgerEntryDto>>.Ok(result));
        }).RequirePermission("Ledger:View");

        group.MapGet("/{id:int}", async (int id, LedgerService service) =>
        {
            var result = await service.GetByIdAsync(id);
            return Results.Ok(ApiResponse<LedgerEntryDto>.Ok(result));
        }).RequirePermission("Ledger:View");

        group.MapPost("/manual", async (CreateManualLedgerEntryRequest request, LedgerService service) =>
        {
            var id = await service.CreateManualEntryAsync(request);
            return Results.Created($"/api/ledger/{id}", ApiResponse<int>.Ok(id, "Ledger entry created."));
        }).RequirePermission("Ledger:Create");
    }
}
