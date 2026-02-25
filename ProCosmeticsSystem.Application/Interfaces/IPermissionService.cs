using ProCosmeticsSystem.Application.DTOs.Auth;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int userId, string permission);
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    Task<List<PermissionDto>> GetPermissionsByRoleAsync(int roleId);
    Task AssignPermissionsToRoleAsync(int roleId, List<int> permissionIds);
}
