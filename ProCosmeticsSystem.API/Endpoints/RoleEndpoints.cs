using Microsoft.AspNetCore.Identity;
using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Auth;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.API.Endpoints;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles").WithTags("Roles").RequireAuthorization();

        group.MapGet("/", async (RoleManager<AppRole> roleManager, IPermissionService permissionService) =>
        {
            var roles = roleManager.Roles.ToList();
            var roleDtos = new List<RoleDto>();
            foreach (var role in roles)
            {
                var permissions = await permissionService.GetPermissionsByRoleAsync(role.Id);
                roleDtos.Add(new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Description = role.Description,
                    Permissions = permissions
                });
            }
            return Results.Ok(ApiResponse<List<RoleDto>>.Ok(roleDtos));
        }).RequirePermission("UserManagement:View");

        group.MapGet("/{id:int}", async (int id, RoleManager<AppRole> roleManager, IPermissionService permissionService) =>
        {
            var role = await roleManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Role", id);

            var permissions = await permissionService.GetPermissionsByRoleAsync(role.Id);
            var dto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                Permissions = permissions
            };
            return Results.Ok(ApiResponse<RoleDto>.Ok(dto));
        }).RequirePermission("UserManagement:View");

        group.MapPost("/", async (CreateRoleRequest request, RoleManager<AppRole> roleManager, IPermissionService permissionService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ValidationException("Name", "Role name is required.");

            var role = new AppRole { Name = request.Name, Description = request.Description };
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new AppException(string.Join(", ", result.Errors.Select(e => e.Description)));

            if (request.PermissionIds.Count > 0)
                await permissionService.AssignPermissionsToRoleAsync(role.Id, request.PermissionIds);

            return Results.Created($"/api/roles/{role.Id}", ApiResponse<RoleDto>.Ok(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description
            }, "Role created successfully."));
        }).RequirePermission("UserManagement:Create");

        group.MapPut("/{id:int}", async (int id, UpdateRoleRequest request, RoleManager<AppRole> roleManager, IPermissionService permissionService) =>
        {
            var role = await roleManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Role", id);

            role.Description = request.Description;
            await roleManager.UpdateAsync(role);
            await permissionService.AssignPermissionsToRoleAsync(role.Id, request.PermissionIds);

            return Results.Ok(ApiResponse.Ok("Role updated successfully."));
        }).RequirePermission("UserManagement:Edit");

        group.MapDelete("/{id:int}", async (int id, RoleManager<AppRole> roleManager) =>
        {
            var role = await roleManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("Role", id);

            await roleManager.DeleteAsync(role);
            return Results.Ok(ApiResponse.Ok("Role deleted successfully."));
        }).RequirePermission("UserManagement:Delete");

        group.MapGet("/permissions", async (IPermissionService permissionService) =>
        {
            var permissions = await permissionService.GetAllPermissionsAsync();
            return Results.Ok(ApiResponse<List<PermissionDto>>.Ok(permissions));
        }).RequirePermission("UserManagement:View");
    }
}
