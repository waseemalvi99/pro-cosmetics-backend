namespace ProCosmeticsSystem.Application.DTOs.Reports;

public class SalesmanPerformanceDto
{
    public int SalesmanId { get; set; }
    public string SalesmanName { get; set; } = string.Empty;
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
}
