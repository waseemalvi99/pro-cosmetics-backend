namespace ProCosmeticsSystem.Application.DTOs.Deliveries;

public class DeliveryDto
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public string? SaleNumber { get; set; }
    public int? DeliveryManId { get; set; }
    public string? DeliveryManName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDeliveryRequest
{
    public int SaleId { get; set; }
    public int? DeliveryManId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
}

public class UpdateDeliveryStatusRequest
{
    public string? Notes { get; set; }
}
