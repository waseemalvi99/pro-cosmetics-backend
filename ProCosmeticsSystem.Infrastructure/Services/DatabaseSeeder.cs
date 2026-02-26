using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Services;

public class DatabaseSeeder
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly DbConnectionFactory _db;
    private readonly ILogger<DatabaseSeeder> _logger;

    // Change these defaults before first run if desired
    private const string AdminEmail    = "admin@procosmetics.com";
    private const string AdminFullName = "System Administrator";
    private const string AdminPassword = "Admin@123!";
    private const string AdminRoleName = "Admin";

    public DatabaseSeeder(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        DbConnectionFactory db,
        ILogger<DatabaseSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedAdminRoleAsync();
        await SeedAdminUserAsync();
        await AssignAllPermissionsToAdminAsync();
    }

    // ── 1. Role ─────────────────────────────────────────────────────────────
    private async Task SeedAdminRoleAsync()
    {
        if (await _roleManager.RoleExistsAsync(AdminRoleName))
            return;

        var result = await _roleManager.CreateAsync(new AppRole
        {
            Name        = AdminRoleName,
            Description = "Full system access"
        });

        if (result.Succeeded)
            _logger.LogInformation("Admin role created.");
        else
            _logger.LogError("Failed to create Admin role: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    // ── 2. User ──────────────────────────────────────────────────────────────
    private async Task SeedAdminUserAsync()
    {
        var existing = await _userManager.FindByEmailAsync(AdminEmail);
        if (existing is not null)
        {
            _logger.LogInformation("Admin user already exists — skipping.");
            return;
        }

        var admin = new AppUser
        {
            FullName         = AdminFullName,
            UserName         = AdminEmail,
            Email            = AdminEmail,
            EmailConfirmed   = true,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(admin, AdminPassword);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await _userManager.AddToRoleAsync(admin, AdminRoleName);
        if (roleResult.Succeeded)
            _logger.LogInformation("Admin user created and assigned to Admin role. Email: {Email}, Password: {Pass}",
                AdminEmail, AdminPassword);
        else
            _logger.LogError("Failed to assign Admin role to admin user: {Errors}",
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
    }

    // ── 3. Permissions → Admin role ──────────────────────────────────────────
    private async Task AssignAllPermissionsToAdminAsync()
    {
        using var conn = _db.CreateConnection();

        // Get the Admin role id
        var roleId = await conn.ExecuteScalarAsync<int?>(
            "SELECT Id FROM AspNetRoles WHERE NormalizedName = @Name",
            new { Name = AdminRoleName.ToUpperInvariant() });

        if (roleId is null)
        {
            _logger.LogWarning("Admin role not found in database — permissions not assigned.");
            return;
        }

        // Count permissions already assigned
        var assigned = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM RolePermissions WHERE RoleId = @RoleId",
            new { RoleId = roleId });

        if (assigned > 0)
        {
            _logger.LogInformation("Admin role already has {Count} permissions — skipping.", assigned);
            return;
        }

        // Assign every permission in the system to Admin
        var affected = await conn.ExecuteAsync(@"
            INSERT INTO RolePermissions (RoleId, PermissionId)
            SELECT @RoleId, Id FROM Permissions
            WHERE Id NOT IN (
                SELECT PermissionId FROM RolePermissions WHERE RoleId = @RoleId
            )",
            new { RoleId = roleId });

        _logger.LogInformation("Assigned {Count} permissions to Admin role.", affected);
    }
}
