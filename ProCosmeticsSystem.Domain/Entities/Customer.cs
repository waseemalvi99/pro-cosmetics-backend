using ProCosmeticsSystem.Domain.Common;

namespace ProCosmeticsSystem.Domain.Entities;

public class Customer : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
