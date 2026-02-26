using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Accounts;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts").RequireAuthorization();

        group.MapGet("/customer/{id:int}/statement", async (int id, DateTime? fromDate, DateTime? toDate, AccountStatementService service) =>
        {
            var result = await service.GetCustomerStatementAsync(id, fromDate, toDate);
            return Results.Ok(ApiResponse<AccountStatementDto>.Ok(result));
        }).RequirePermission("Accounts:View");

        group.MapGet("/supplier/{id:int}/statement", async (int id, DateTime? fromDate, DateTime? toDate, AccountStatementService service) =>
        {
            var result = await service.GetSupplierStatementAsync(id, fromDate, toDate);
            return Results.Ok(ApiResponse<AccountStatementDto>.Ok(result));
        }).RequirePermission("Accounts:View");

        group.MapGet("/customer/{id:int}/statement/pdf", async (int id, DateTime? fromDate, DateTime? toDate, AccountStatementService service) =>
        {
            var pdf = await service.GetCustomerStatementPdfAsync(id, fromDate, toDate);
            return Results.File(pdf, "application/pdf", $"customer-statement-{id}.pdf");
        }).RequirePermission("Accounts:Export");

        group.MapGet("/supplier/{id:int}/statement/pdf", async (int id, DateTime? fromDate, DateTime? toDate, AccountStatementService service) =>
        {
            var pdf = await service.GetSupplierStatementPdfAsync(id, fromDate, toDate);
            return Results.File(pdf, "application/pdf", $"supplier-statement-{id}.pdf");
        }).RequirePermission("Accounts:Export");

        group.MapGet("/aging/receivables", async (AccountStatementService service) =>
        {
            var result = await service.GetReceivablesAgingAsync();
            return Results.Ok(ApiResponse<AgingReportDto>.Ok(result));
        }).RequirePermission("Accounts:View");

        group.MapGet("/aging/payables", async (AccountStatementService service) =>
        {
            var result = await service.GetPayablesAgingAsync();
            return Results.Ok(ApiResponse<AgingReportDto>.Ok(result));
        }).RequirePermission("Accounts:View");
    }
}
