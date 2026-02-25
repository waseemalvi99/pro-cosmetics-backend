using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Infrastructure.Persistence;

public class AppIdentityDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<AppRole>(e =>
        {
            e.Property(r => r.Description).HasMaxLength(500);
        });
    }
}
