using Dapper;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly DbConnectionFactory _db;

    public PermissionRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Permission>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Permission>("SELECT Id, Name, Module, Description FROM Permissions ORDER BY Module, Name");
    }

    public async Task<IEnumerable<Permission>> GetByRoleIdAsync(int roleId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Permission>(
            @"SELECT p.Id, p.Name, p.Module, p.Description
              FROM Permissions p
              INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
              WHERE rp.RoleId = @RoleId
              ORDER BY p.Module, p.Name",
            new { RoleId = roleId });
    }

    public async Task<IEnumerable<string>> GetPermissionNamesByUserIdAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<string>(
            @"SELECT DISTINCT p.Name
              FROM Permissions p
              INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
              INNER JOIN AspNetUserRoles ur ON rp.RoleId = ur.RoleId
              WHERE ur.UserId = @UserId",
            new { UserId = userId });
    }

    public async Task AssignToRoleAsync(int roleId, IEnumerable<int> permissionIds)
    {
        using var conn = _db.CreateConnection();
        foreach (var permissionId in permissionIds)
        {
            await conn.ExecuteAsync(
                "INSERT INTO RolePermissions (RoleId, PermissionId) VALUES (@RoleId, @PermissionId)",
                new { RoleId = roleId, PermissionId = permissionId });
        }
    }

    public async Task RemoveFromRoleAsync(int roleId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM RolePermissions WHERE RoleId = @RoleId", new { RoleId = roleId });
    }
}
