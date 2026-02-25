using ProCosmeticsSystem.Domain.Common;

namespace ProCosmeticsSystem.Domain.Entities;

public class DeliveryMan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
