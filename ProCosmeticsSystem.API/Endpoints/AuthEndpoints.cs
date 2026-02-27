using ProCosmeticsSystem.Application.DTOs.Auth;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.Interfaces;


namespace ProCosmeticsSystem.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);
            return Results.Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
        }).AllowAnonymous();

        group.MapPost("/refresh-token", async (RefreshTokenRequest request, IAuthService authService) =>
        {
            var result = await authService.RefreshTokenAsync(request);
            return Results.Ok(ApiResponse<LoginResponse>.Ok(result, "Token refreshed successfully."));
        }).AllowAnonymous();
    }
}
