using DigitalWallet.Application.DTOs.User;

namespace DigitalWallet.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(int userId);

    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);

    Task ChangePasswordAsync(int userId, UpdateProfileDto dto);
}
