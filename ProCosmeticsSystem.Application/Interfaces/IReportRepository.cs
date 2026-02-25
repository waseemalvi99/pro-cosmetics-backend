using ProCosmeticsSystem.Application.DTOs.Reports;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IReportRepository
{
    Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, string groupBy);
    Task<List<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, int top);
    Task<List<SalesmanPerformanceDto>> GetSalesmanPerformanceAsync(DateTime from, DateTime to);
    Task<InventoryReportDto> GetInventoryReportAsync();
    Task<PurchaseReportDto> GetPurchaseReportAsync(DateTime from, DateTime to);
    Task<DeliveryReportDto> GetDeliveryReportAsync(DateTime from, DateTime to);
    Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime from, DateTime to);
}
