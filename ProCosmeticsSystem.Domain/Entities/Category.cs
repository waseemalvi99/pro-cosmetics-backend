using ProCosmeticsSystem.Domain.Common;

namespace ProCosmeticsSystem.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }
}
