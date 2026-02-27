using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ProCosmeticsSystem.Application.DTOs.Auth;
using ProCosmeticsSystem.Application.Exceptions;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Domain.Entities;

namespace ProCosmeticsSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IPermissionRepository _permissionRepo;
    private readonly IConfiguration _configuration;
    private readonly IEmailNotificationService _emailNotification;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IPermissionRepository permissionRepo,
        IConfiguration configuration,
        IEmailNotificationService emailNotification,
        IFileStorageService fileStorage,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _permissionRepo = permissionRepo;
        _emailNotification = emailNotification;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new AppException("Invalid email or password.", 401);

        if (!user.IsActive)
            throw new AppException("Account is deactivated.", 403);

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            throw new AppException("Invalid email or password.", 401);

        var token = await GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationInDays"));
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionRepo.GetPermissionNamesByUserIdAsync(user.Id);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>("JwtSettings:ExpirationInMinutes")),
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                Permissions = permissions.ToList(),
                CreatedAt = user.CreatedAt,
                ProfilePicture = user.ProfilePicture
            }
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterUserRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new ValidationException("Email", "A user with this email already exists.");

        var password = GenerateRandomPassword();

        var user = new AppUser
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new ValidationException(errors);
        }

        if (!string.IsNullOrEmpty(request.RoleName))
        {
            if (await _roleManager.RoleExistsAsync(request.RoleName))
                await _userManager.AddToRoleAsync(user, request.RoleName);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionRepo.GetPermissionNamesByUserIdAsync(user.Id);

        _logger.LogInformation("User {Email} created successfully (Id: {UserId}), sending welcome email with credentials",
            user.Email, user.Id);
        _emailNotification.NotifyUserCreated(user.FullName, user.Email!, password);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            IsActive = user.IsActive,
            Roles = roles.ToList(),
            Permissions = permissions.ToList(),
            CreatedAt = user.CreatedAt,
            ProfilePicture = user.ProfilePicture
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.Token);
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AppException("Invalid token.", 401);

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new AppException("Invalid token.", 401);

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new AppException("Invalid or expired refresh token.", 401);

        var newToken = await GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationInDays"));
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionRepo.GetPermissionNamesByUserIdAsync(user.Id);

        return new LoginResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>("JwtSettings:ExpirationInMinutes")),
            User = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                Permissions = permissions.ToList(),
                CreatedAt = user.CreatedAt,
                ProfilePicture = user.ProfilePicture
            }
        };
    }

    // ── Profile Management ─────────────────────────────────────────────

    public async Task<UserDto> GetProfileAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new AppException("User not found.", 404);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionRepo.GetPermissionNamesByUserIdAsync(user.Id);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            IsActive = user.IsActive,
            Roles = roles.ToList(),
            Permissions = permissions.ToList(),
            CreatedAt = user.CreatedAt,
            ProfilePicture = user.ProfilePicture
        };
    }

    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ValidationException("FullName", "Full name is required.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new AppException("User not found.", 404);

        user.FullName = request.FullName.Trim();
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionRepo.GetPermissionNamesByUserIdAsync(user.Id);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            IsActive = user.IsActive,
            Roles = roles.ToList(),
            Permissions = permissions.ToList(),
            CreatedAt = user.CreatedAt,
            ProfilePicture = user.ProfilePicture
        };
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new AppException("User not found.", 404);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new AppException(errors, 400);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return; // Don't leak user existence

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        _emailNotification.NotifyPasswordReset(user.FullName, user.Email!, token);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new AppException("User not found.", 404);

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new AppException(errors, 400);
        }
    }

    public async Task<string> UploadProfilePictureAsync(int userId, Stream imageStream, string fileName)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            throw new ValidationException("File", "Only .jpg, .jpeg, .png, .webp images are allowed.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new AppException("User not found.", 404);

        // Delete old profile picture if exists
        if (!string.IsNullOrEmpty(user.ProfilePicture))
        {
            _fileStorage.DeleteFile($"uploads/profiles/{user.ProfilePicture}");
        }

        var (savedFileName, _) = await _fileStorage.SaveImageAsync(imageStream, fileName, "profiles", 400, 400, 85);

        user.ProfilePicture = savedFileName;
        await _userManager.UpdateAsync(user);

        return $"/uploads/profiles/{savedFileName}";
    }

    public async Task RemoveProfilePictureAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new AppException("User not found.", 404);

        if (!string.IsNullOrEmpty(user.ProfilePicture))
        {
            _fileStorage.DeleteFile($"uploads/profiles/{user.ProfilePicture}");
            user.ProfilePicture = null;
            await _userManager.UpdateAsync(user);
        }
    }

    // ── Private Helpers ──────────────────────────────────────────────────

    private async Task<string> GenerateJwtToken(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionRepo.GetPermissionNamesByUserIdAsync(user.Id);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _configuration.GetValue<int>("JwtSettings:ExpirationInMinutes")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!)),
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new AppException("Invalid token.", 401);

        return principal;
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static string GenerateRandomPassword(int length = 12)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%&*";
        const string all = upper + lower + digits + special;

        using var rng = RandomNumberGenerator.Create();
        var password = new char[length];

        // Ensure at least one of each category
        password[0] = Pick(rng, upper);
        password[1] = Pick(rng, lower);
        password[2] = Pick(rng, digits);
        password[3] = Pick(rng, special);

        for (int i = 4; i < length; i++)
            password[i] = Pick(rng, all);

        // Shuffle to avoid predictable positions
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        for (int i = length - 1; i > 0; i--)
        {
            int j = bytes[i] % (i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);

        static char Pick(RandomNumberGenerator rng, string chars)
        {
            var buf = new byte[1];
            rng.GetBytes(buf);
            return chars[buf[0] % chars.Length];
        }
    }
}
