using DigitalWallet.Application.DTOs;
using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int id, int userId);
    Task<Transaction?> GetByReferenceNumberAsync(string referenceNumber, int userId);
    Task<Transaction?> GetByIdempotencyKeyAsync(int walletId, string key);
    Task<List<Transaction>> GetByWalletIdAsync(int walletId, TransactionFilterDto filter);
    Task<int> GetTotalCountByWalletIdAsync(int walletId, TransactionFilterDto filter);
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<decimal> GetOutgoingTotalAsync(int walletId, DateTime startUtc, DateTime endUtc);
}
