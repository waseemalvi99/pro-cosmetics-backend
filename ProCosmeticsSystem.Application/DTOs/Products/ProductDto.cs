namespace ProCosmeticsSystem.Application.DTOs.Products;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
    public int QuantityOnHand { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProductImageDto> Images { get; set; } = [];
}
