using SGE.Application.DTOs.Users;

namespace SGE.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<bool> LogoutAsync(string userId);
    Task<bool> RevokeTokenAsync(string token);
    Task<UserDto?> GetCurrentUserAsync(string userId);
    Task<UserDto> UpdateUserAsync(string userId, UpdateUserDto updateDto);
    Task<bool> DeleteUserAsync(string userId);
}
