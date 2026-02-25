using ProCosmeticsSystem.Domain.Common;

namespace ProCosmeticsSystem.Domain.Entities;

public class Salesman : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; } = true;
}
