using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int id);
    Task<Transaction?> GetByReferenceNumberAsync(string referenceNumber);
    Task<List<Transaction>> GetByWalletIdAsync(int walletId, int page, int pageSize);
    Task<int> GetTotalCountByWalletIdAsync(int walletId);
    Task<Transaction> CreateAsync(Transaction transaction);
}
