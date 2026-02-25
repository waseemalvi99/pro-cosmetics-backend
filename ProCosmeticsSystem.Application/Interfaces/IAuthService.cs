using ProCosmeticsSystem.Application.DTOs.Auth;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserDto> RegisterAsync(RegisterUserRequest request);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
