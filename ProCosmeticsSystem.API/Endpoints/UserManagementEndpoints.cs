using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ProCosmeticsSystem.API.Middlewares;
using ProCosmeticsSystem.Application.DTOs.Auth;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.API.Endpoints;

public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("User Management").RequireAuthorization();

        group.MapGet("/", async (int page, int pageSize, string? search, IUserRepository userRepo) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
            var result = await userRepo.GetUsersAsync(page, pageSize, search);
            return Results.Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
        }).RequirePermission("UserManagement:View");

        group.MapGet("/{id:int}", async (int id, IUserRepository userRepo) =>
        {
            var user = await userRepo.GetUserByIdAsync(id)
                ?? throw new NotFoundException("User", id);
            return Results.Ok(ApiResponse<UserDto>.Ok(user));
        }).RequirePermission("UserManagement:View");

        group.MapPost("/register", async (RegisterUserRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            return Results.Ok(ApiResponse<UserDto>.Ok(result, "User created successfully. Credentials sent via email."));
        }).RequirePermission("UserManagement:Create");

        group.MapPost("/{id:int}/assign-role", async (int id, AssignRoleRequest request,
            UserManager<AppUser> userManager, RoleManager<AppRole> roleManager) =>
        {
            var user = await userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("User", id);

            if (!await roleManager.RoleExistsAsync(request.RoleName))
                throw new ValidationException("RoleName", "Role does not exist.");

            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, request.RoleName);

            return Results.Ok(ApiResponse.Ok("Role assigned successfully."));
        }).RequirePermission("UserManagement:Edit");

        group.MapPut("/{id:int}/toggle-active", async (int id, HttpContext httpContext, UserManager<AppUser> userManager) =>
        {
            var currentUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == id.ToString())
                throw new AppException("You cannot deactivate your own account.", 400);

            var user = await userManager.FindByIdAsync(id.ToString())
                ?? throw new NotFoundException("User", id);

            user.IsActive = !user.IsActive;
            await userManager.UpdateAsync(user);

            return Results.Ok(ApiResponse.Ok($"User {(user.IsActive ? "activated" : "deactivated")} successfully."));
        }).RequirePermission("UserManagement:Edit");
    }
}
