namespace ProCosmeticsSystem.Application.DTOs.Combos;

public class ComboItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CustomerComboDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
}

public class SupplierComboDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PaymentTermDays { get; set; }
}

public class ProductComboDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int QuantityOnHand { get; set; }
    public string? ImagePath { get; set; }
}

public class DeliveryManComboDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

public class SaleComboDto
{
    public int Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime SaleDate { get; set; }
}

public class PurchaseOrderComboDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}
