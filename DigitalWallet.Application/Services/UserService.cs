


using DigitalWallet.Application.DTOs.User;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        User? user = await _userRepository.GetByIdAsync(userId);

        if(user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        return MapToDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        User? user = await _userRepository.GetByIdAsync(userId);

        if(user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;

        User updated = await _userRepository.UpdateAsync(user);

        return MapToDto(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        User? user = await _userRepository.GetByIdAsync(userId);

        if(user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        bool currentPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);

        if (!currentPasswordValid)
            throw new WalletException("Current password is incorrect");

        if (dto.NewPassword != dto.ConfirmPassword)
            throw new WalletException("New password and confirm password do not match");

        bool isSamePassword = BCrypt.Net.BCrypt.Verify(
            dto.NewPassword,
            user.PasswordHash);

        if (isSamePassword)
            throw new WalletException("New password must be different from current password");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user);
        
    }

    private static UserProfileDto MapToDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
        };
    }

}