using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using SGE.Application.DTOs.Users;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;
using SGE.Core.Exceptions;

namespace SGE.Application.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IMapper mapper) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var existingUserByEmail = await userManager.FindByEmailAsync(registerDto.Email);
        if (existingUserByEmail != null)
            throw new UserAlreadyExistsException(registerDto.Email, "email");

        var existingUserByUsername = await userManager.FindByNameAsync(registerDto.UserName);
        if (existingUserByUsername != null)
            throw new UserAlreadyExistsException(registerDto.UserName, "nom d'utilisateur");

        var user = mapper.Map<ApplicationUser>(registerDto);

        var result = await userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            throw new UserRegistrationException(errors);
        }

        await userManager.AddToRoleAsync(user, "User");

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await tokenService.CreateRefreshTokenAsync(user.Id);

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = userDto
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, loginDto.Password))
            throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new UserNotActiveException();

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await tokenService.CreateRefreshTokenAsync(user.Id);

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = userDto
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(refreshTokenDto.AccessToken);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new InvalidRefreshTokenException();

        var isValidRefreshToken = await tokenService.ValidateRefreshTokenAsync(refreshTokenDto.RefreshToken, userId);
        if (!isValidRefreshToken)
            throw new InvalidRefreshTokenException();

        var user = await userManager.FindByIdAsync(userId);
        if (user is not { IsActive: true })
            throw new UserNotActiveException();

        await tokenService.RevokeRefreshTokenAsync(refreshTokenDto.RefreshToken, "Remplacé par un nouveau token");

        var roles = await userManager.GetRolesAsync(user);
        var newAccessToken = tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = await tokenService.CreateRefreshTokenAsync(user.Id);

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = userDto
        };
    }

    public async Task<bool> LogoutAsync(string userId)
    {
        await tokenService.RevokeAllUserRefreshTokensAsync(userId);
        return true;
    }

    public async Task<bool> RevokeTokenAsync(string token)
    {
        await tokenService.RevokeRefreshTokenAsync(token, "Révoqué manuellement");
        return true;
    }

    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = await userManager.GetRolesAsync(user);

        return userDto;
    }

    public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserDto updateDto)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidCredentialsException();

        if (!string.IsNullOrWhiteSpace(updateDto.FirstName))
            user.FirstName = updateDto.FirstName;

        if (!string.IsNullOrWhiteSpace(updateDto.LastName))
            user.LastName = updateDto.LastName;

        if (!string.IsNullOrWhiteSpace(updateDto.Email) && updateDto.Email != user.Email)
        {
            var existingUser = await userManager.FindByEmailAsync(updateDto.Email);
            if (existingUser != null)
                throw new UserAlreadyExistsException(updateDto.Email, "email");
            user.Email = updateDto.Email;
            user.UserName = updateDto.Email;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
            user.PhoneNumber = updateDto.PhoneNumber;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            throw new UserRegistrationException(errors);
        }

        var userDto = mapper.Map<UserDto>(user);
        userDto.Roles = await userManager.GetRolesAsync(user);

        return userDto;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        await tokenService.RevokeAllUserRefreshTokensAsync(userId);

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}
