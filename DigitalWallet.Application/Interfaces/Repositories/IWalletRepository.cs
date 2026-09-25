using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(int id);
    Task<Wallet?> GetByUserIdAsync(int userId);
    Task<Wallet?> GetByWalletNumberAsync(string walletNumber);
    Task<bool> ExistsByUserIdAsync(int userId);
    Task<Wallet> CreateAsync(Wallet wallet);
    Task<Wallet> UpdateAsync(Wallet wallet);
}
