namespace ProCosmeticsSystem.Application.DTOs.Auth;

public class AssignRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}

public class AssignPermissionsRequest
{
    public List<int> PermissionIds { get; set; } = [];
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int> PermissionIds { get; set; } = [];
}

public class UpdateRoleRequest
{
    public string? Description { get; set; }
    public List<int> PermissionIds { get; set; } = [];
}
