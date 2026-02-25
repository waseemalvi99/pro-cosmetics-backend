namespace ProCosmeticsSystem.Application.DTOs.Products;

public class InventoryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int AvailableQuantity => QuantityOnHand - QuantityReserved;
    public int ReorderLevel { get; set; }
    public bool IsLowStock => QuantityOnHand <= ReorderLevel;
    public DateTime? LastRestockedAt { get; set; }
}

public class AdjustInventoryRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}
