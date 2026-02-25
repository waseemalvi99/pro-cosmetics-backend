using ProCosmeticsSystem.Application.DTOs.Auth;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepo;

    public PermissionService(IPermissionRepository permissionRepo)
    {
        _permissionRepo = permissionRepo;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        var permissions = await _permissionRepo.GetPermissionNamesByUserIdAsync(userId);
        return permissions.Contains(permission);
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync()
    {
        var permissions = await _permissionRepo.GetAllAsync();
        return permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Module = p.Module,
            Description = p.Description
        }).ToList();
    }

    public async Task<List<PermissionDto>> GetPermissionsByRoleAsync(int roleId)
    {
        var permissions = await _permissionRepo.GetByRoleIdAsync(roleId);
        return permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Module = p.Module,
            Description = p.Description
        }).ToList();
    }

    public async Task AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds)
    {
        await _permissionRepo.RemoveFromRoleAsync(roleId);
        if (permissionIds.Count > 0)
            await _permissionRepo.AssignToRoleAsync(roleId, permissionIds);
    }
}
