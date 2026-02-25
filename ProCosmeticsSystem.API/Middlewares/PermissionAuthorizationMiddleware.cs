using System.Security.Claims;

namespace ProCosmeticsSystem.API.Middlewares;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    public string Permission { get; }
    public RequirePermissionAttribute(string permission) => Permission = permission;
}

public static class PermissionAuthorizationExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
                return Results.Unauthorized();

            var permissions = user.FindAll("permission").Select(c => c.Value);
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);

            if (roles.Contains("Admin") || permissions.Contains(permission))
                return await next(context);

            return Results.Forbid();
        });
    }
}
