namespace ProCosmeticsSystem.Application.DTOs.Reports;

public class SalesReportDto
{
    public List<SalesReportItem> Items { get; set; } = [];
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class SalesReportItem
{
    public string Period { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal Discount { get; set; }
    public decimal NetRevenue { get; set; }
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
