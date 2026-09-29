using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await _context
            .Transactions.Include(t => t.Wallet)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Transaction?> GetByReferenceNumberAsync(string referenceNumber)
    {
        return await _context.Transactions.FirstOrDefaultAsync(t =>
            t.ReferenceNumber == referenceNumber
        );
    }

    public async Task<List<Transaction>> GetByWalletIdAsync(int walletId, int page, int pageSize)
    {
        return await _context
            .Transactions.Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountByWalletIdAsync(int walletId)
    {
        return await _context.Transactions.CountAsync(t => t.WalletId == walletId);
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }
}
