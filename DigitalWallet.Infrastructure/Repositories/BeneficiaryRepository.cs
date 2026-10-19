using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Repositories;

public class BeneficiaryRepository : IBeneficiaryRepository
{
    private readonly AppDbContext _context;

    public BeneficiaryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Beneficiary>> GetAllByUserIdAsync(int userId)
    {
        return await _context
            .Beneficiaries.Where(b => b.UserId == userId)
            .OrderBy(b => b.Nickname)
            .ToListAsync();
    }

    public async Task<Beneficiary?> GetByIdAsync(int id)
    {
        return await _context.Beneficiaries.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<bool> ExistsAsync(int userId, string walletNumber)
    {
        return await _context.Beneficiaries.AnyAsync(b =>
            b.UserId == userId && b.WalletNumber == walletNumber
        );
    }

    public async Task<Beneficiary> AddAsync(Beneficiary beneficiary)
    {
        _context.Beneficiaries.Add(beneficiary);
        await _context.SaveChangesAsync();
        return beneficiary;
    }

    public async Task DeleteAsync(Beneficiary beneficiary)
    {
        _context.Beneficiaries.Remove(beneficiary);
        await _context.SaveChangesAsync();
    }
}
