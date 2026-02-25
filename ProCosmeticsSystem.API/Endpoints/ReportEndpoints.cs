using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.DTOs.Reports;
using ProCosmeticsSystem.Application.Services;

namespace ProCosmeticsSystem.API.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization();

        group.MapGet("/sales", async (DateTime from, DateTime to, string? groupBy, ReportService service) =>
        {
            var result = await service.GetSalesReportAsync(from, to, groupBy ?? "day");
            return Results.Ok(ApiResponse<SalesReportDto>.Ok(result));
        }).RequirePermission("Reports:View");

        group.MapGet("/top-products", async (DateTime from, DateTime to, int? top, ReportService service) =>
        {
            var result = await service.GetTopProductsAsync(from, to, top ?? 10);
            return Results.Ok(ApiResponse<List<TopProductDto>>.Ok(result));
        }).RequirePermission("Reports:View");

        group.MapGet("/salesman-performance", async (DateTime from, DateTime to, ReportService service) =>
        {
            var result = await service.GetSalesmanPerformanceAsync(from, to);
            return Results.Ok(ApiResponse<List<SalesmanPerformanceDto>>.Ok(result));
        }).RequirePermission("Reports:View");

        group.MapGet("/inventory", async (ReportService service) =>
        {
            var result = await service.GetInventoryReportAsync();
            return Results.Ok(ApiResponse<InventoryReportDto>.Ok(result));
        }).RequirePermission("Reports:View");

        group.MapGet("/purchases", async (DateTime from, DateTime to, ReportService service) =>
        {
            var result = await service.GetPurchaseReportAsync(from, to);
            return Results.Ok(ApiResponse<PurchaseReportDto>.Ok(result));
        }).RequirePermission("Reports:View");

        group.MapGet("/deliveries", async (DateTime from, DateTime to, ReportService service) =>
        {
            var result = await service.GetDeliveryReportAsync(from, to);
            return Results.Ok(ApiResponse<DeliveryReportDto>.Ok(result));
        }).RequirePermission("Reports:View");

        group.MapGet("/financial-summary", async (DateTime from, DateTime to, ReportService service) =>
        {
            var result = await service.GetFinancialSummaryAsync(from, to);
            return Results.Ok(ApiResponse<FinancialSummaryDto>.Ok(result));
        }).RequirePermission("Reports:View");
    }
}
