using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User> CreateAsync(User user);
}
