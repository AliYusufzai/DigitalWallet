using DigitalWallet.Application.DTOs;
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

    public async Task<List<Transaction>> GetByWalletIdAsync(
        int walletId,
        TransactionFilterDto filter
    )
    {
        IQueryable<Transaction> query = _context.Transactions.Where(t => t.Id == walletId);

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= filter.To.Value.AddDays(1));
        }
        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountByWalletIdAsync(int walletId, TransactionFilterDto filter)
    {
        IQueryable<Transaction> query = _context.Transactions.Where(t => t.Id == walletId);

        if (filter.Type.HasValue)
            query = query.Where(t => t.Type == filter.Type.Value);

        if (filter.From.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.To.Value.AddDays(1));

        return await query.CountAsync();
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }
}
