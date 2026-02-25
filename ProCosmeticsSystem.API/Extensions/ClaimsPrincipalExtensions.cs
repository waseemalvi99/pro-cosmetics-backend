using System.Security.Claims;

namespace ProCosmeticsSystem.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Email);
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    public static IEnumerable<string> GetPermissions(this ClaimsPrincipal principal)
    {
        return principal.FindAll("permission").Select(c => c.Value);
    }

    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
    {
        return principal.GetRoles().Contains("Admin") || principal.GetPermissions().Contains(permission);
    }
}
