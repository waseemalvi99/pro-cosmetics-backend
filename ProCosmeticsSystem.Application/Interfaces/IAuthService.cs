using ProCosmeticsSystem.Application.DTOs.Auth;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserDto> RegisterAsync(RegisterUserRequest request);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);

    // Profile management
    Task<UserDto> GetProfileAsync(int userId);
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task<string> UploadProfilePictureAsync(int userId, Stream imageStream, string fileName);
    Task RemoveProfilePictureAsync(int userId);
}
