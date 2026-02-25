using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IPermissionRepository
{
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<Permission>> GetByRoleIdAsync(int roleId);
    Task<IEnumerable<string>> GetPermissionNamesByUserIdAsync(int userId);
    Task AssignToRoleAsync(int roleId, IEnumerable<int> permissionIds);
    Task RemoveFromRoleAsync(int roleId);
}
