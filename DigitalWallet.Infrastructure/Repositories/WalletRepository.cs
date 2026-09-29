using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _context;

    public WalletRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByIdAsync(int id)
    {
        return await _context.Wallets.Include(w => w.User).FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<Wallet?> GetByUserIdAsync(int userId)
    {
        return await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task<Wallet?> GetByWalletNumberAsync(string walletNumber)
    {
        return await _context.Wallets.FirstOrDefaultAsync(w => w.WalletNumber == walletNumber);
    }

    public async Task<bool> ExistsByUserIdAsync(int userId)
    {
        return await _context.Wallets.AnyAsync(w => w.UserId == userId);
    }

    public async Task<Wallet> CreateAsync(Wallet wallet)
    {
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();
        return wallet;
    }

    public async Task<Wallet> UpdateAsync(Wallet wallet)
    {
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        return wallet;
    }
}
