namespace ProCosmeticsSystem.Application.DTOs.Reports;

public class FinancialSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCosts { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public int TotalSales { get; set; }
    public int TotalPurchases { get; set; }
}
