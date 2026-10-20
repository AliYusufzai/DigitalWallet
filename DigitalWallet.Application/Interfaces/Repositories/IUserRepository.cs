using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);

    Task<bool> TrySetVerificationTokenAsync(int userId, string tokenHash, DateTime now);
    Task<bool> TryVerifyEmailAsync(string email, string tokenHash, DateTime now);
}
