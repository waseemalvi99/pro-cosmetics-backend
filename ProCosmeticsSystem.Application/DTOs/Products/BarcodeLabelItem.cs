namespace ProCosmeticsSystem.Application.DTOs.Products;

public class BarcodeLabelItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
}
