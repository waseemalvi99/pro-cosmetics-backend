using ProCosmeticsSystem.Domain.Common;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Domain.Entities;

public class Delivery : AuditableEntity
{
    public int SaleId { get; set; }
    public int? DeliveryManId { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
}
