using ProCosmeticsSystem.Domain.Common;
using ProCosmeticsSystem.Domain.Enums;

namespace ProCosmeticsSystem.Domain.Entities;

public class PurchaseOrder : AuditableEntity
{
    public int SupplierId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}
