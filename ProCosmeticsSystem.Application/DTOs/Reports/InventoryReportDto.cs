namespace ProCosmeticsSystem.Application.DTOs.Reports;

public class InventoryReportDto
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public List<InventoryReportItem> Items { get; set; } = [];
}

public class InventoryReportItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public int QuantityOnHand { get; set; }
    public decimal CostPrice { get; set; }
    public decimal StockValue { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsLowStock { get; set; }
}
