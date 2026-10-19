using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace DigitalWallet.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
            throw new InvalidOperationException("A transaction is already active.");
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction is active.");
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync()
    {
        if (_transaction == null)
            return;
        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            // Rolled-back SaveChanges calls have already changed tracking state.
            _context.ChangeTracker.Clear();
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}
