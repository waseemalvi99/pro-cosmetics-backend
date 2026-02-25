using ProCosmeticsSystem.API.Extensions;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User.GetUserId();
            return id == 0 ? null : id;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.GetEmail();

    public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User.GetRoles() ?? [];

    public IEnumerable<string> Permissions => _httpContextAccessor.HttpContext?.User.GetPermissions() ?? [];

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
