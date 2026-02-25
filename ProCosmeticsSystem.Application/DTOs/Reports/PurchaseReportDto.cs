namespace ProCosmeticsSystem.Application.DTOs.Reports;

public class PurchaseReportDto
{
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public List<PurchaseReportItem> Items { get; set; } = [];
}

public class PurchaseReportItem
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}
