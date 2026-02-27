using ProCosmeticsSystem.Application.DTOs.Auth;
using ProCosmeticsSystem.Application.DTOs.Common;

namespace ProCosmeticsSystem.Application.Interfaces;

public interface IUserRepository
{
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<List<string>> GetAdminEmailsAsync();
}
