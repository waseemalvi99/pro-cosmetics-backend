using Microsoft.AspNetCore.Identity;

namespace ProCosmeticsSystem.Domain.Entities;

public class AppRole : IdentityRole<int>
{
    public string? Description { get; set; }
}
