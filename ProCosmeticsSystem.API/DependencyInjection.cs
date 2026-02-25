using ProCosmeticsSystem.API.Services;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Infrastructure.Services;

namespace ProCosmeticsSystem.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IFileStorageService>(new LocalFileStorageService(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot")));

        return services;
    }
}
