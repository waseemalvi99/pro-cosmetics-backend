namespace ProCosmeticsSystem.Application.DTOs.Products;

public class BarcodeLookupResult
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int QuantityOnHand { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
}
