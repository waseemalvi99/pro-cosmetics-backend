using ProCosmeticsSystem.Application.DTOs.Reports;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.Application.Services;

public class ReportService
{
    private readonly IReportRepository _repo;

    public ReportService(IReportRepository repo)
    {
        _repo = repo;
    }

    public Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, string groupBy) =>
        _repo.GetSalesReportAsync(from, to, groupBy);

    public Task<List<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, int top = 10) =>
        _repo.GetTopProductsAsync(from, to, top);

    public Task<List<SalesmanPerformanceDto>> GetSalesmanPerformanceAsync(DateTime from, DateTime to) =>
        _repo.GetSalesmanPerformanceAsync(from, to);

    public Task<InventoryReportDto> GetInventoryReportAsync() =>
        _repo.GetInventoryReportAsync();

    public Task<PurchaseReportDto> GetPurchaseReportAsync(DateTime from, DateTime to) =>
        _repo.GetPurchaseReportAsync(from, to);

    public Task<DeliveryReportDto> GetDeliveryReportAsync(DateTime from, DateTime to) =>
        _repo.GetDeliveryReportAsync(from, to);

    public Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime from, DateTime to) =>
        _repo.GetFinancialSummaryAsync(from, to);
}
