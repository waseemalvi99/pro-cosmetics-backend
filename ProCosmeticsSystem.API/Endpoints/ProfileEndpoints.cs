using ProCosmeticsSystem.Application.DTOs.Auth;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.Interfaces;

namespace ProCosmeticsSystem.API.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var profile = app.MapGroup("/api/profile").WithTags("Profile").RequireAuthorization();

        profile.MapGet("/", async (IAuthService authService, ICurrentUserService currentUser) =>
        {
            var result = await authService.GetProfileAsync(currentUser.UserId!.Value);
            return Results.Ok(ApiResponse<UserDto>.Ok(result));
        });

        profile.MapPut("/", async (UpdateProfileRequest request, IAuthService authService, ICurrentUserService currentUser) =>
        {
            var result = await authService.UpdateProfileAsync(currentUser.UserId!.Value, request);
            return Results.Ok(ApiResponse<UserDto>.Ok(result, "Profile updated successfully."));
        });

        profile.MapPut("/password", async (ChangePasswordRequest request, IAuthService authService, ICurrentUserService currentUser) =>
        {
            await authService.ChangePasswordAsync(currentUser.UserId!.Value, request);
            return Results.Ok(ApiResponse.Ok("Password changed successfully."));
        });

        profile.MapPost("/picture", async (IFormFile file, IAuthService authService, ICurrentUserService currentUser) =>
        {
            using var stream = file.OpenReadStream();
            var url = await authService.UploadProfilePictureAsync(currentUser.UserId!.Value, stream, file.FileName);
            return Results.Ok(ApiResponse<string>.Ok(url, "Profile picture uploaded successfully."));
        }).DisableAntiforgery();

        profile.MapDelete("/picture", async (IAuthService authService, ICurrentUserService currentUser) =>
        {
            await authService.RemoveProfilePictureAsync(currentUser.UserId!.Value);
            return Results.Ok(ApiResponse.Ok("Profile picture removed successfully."));
        });

        // Auth group for anonymous password reset endpoints
        var auth = app.MapGroup("/api/auth").WithTags("Authentication");

        auth.MapPost("/forgot-password", async (ForgotPasswordRequest request, IAuthService authService) =>
        {
            await authService.ForgotPasswordAsync(request);
            return Results.Ok(ApiResponse.Ok("If an account with that email exists, a password reset code has been sent."));
        }).AllowAnonymous();

        auth.MapPost("/reset-password", async (ResetPasswordRequest request, IAuthService authService) =>
        {
            await authService.ResetPasswordAsync(request);
            return Results.Ok(ApiResponse.Ok("Password has been reset successfully."));
        }).AllowAnonymous();
    }
}
