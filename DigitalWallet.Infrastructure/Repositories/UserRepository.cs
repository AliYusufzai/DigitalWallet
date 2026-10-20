using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> TrySetVerificationTokenAsync(int userId, string tokenHash, DateTime now)
    {
        DateTime coolDown = now.AddSeconds(-60);
        DateTime expiresAt = now.AddHours(1);

        int changedRows = await _context
            .Users.Where(user =>
                user.Id == userId
                && user.IsActive
                && user.EmailVerifiedAt == null
                && (
                    user.EmailVerificationLastSentAt == null
                    || user.EmailVerificationLastSentAt <= coolDown
                )
            )
            .ExecuteUpdateAsync(update =>
                update
                    .SetProperty(user => user.EmailVerificationTokenHash, tokenHash)
                    .SetProperty(user => user.EmailVerificationExpiresAt, (DateTime?)expiresAt)
                    .SetProperty(user => user.EmailVerificationLastSentAt, (DateTime?)now)
            );
        return changedRows == 1;
    }

    public async Task<bool> TryVerifyEmailAsync(string email, string tokenHash, DateTime now)
    {
        int changedRows = await _context
            .Users.Where(user =>
                user.Email == email
                && user.IsActive
                && user.EmailVerifiedAt == null
                && user.EmailVerificationTokenHash == tokenHash
                && user.EmailVerificationExpiresAt > now
            )
            .ExecuteUpdateAsync(update =>
                update
                    .SetProperty(user => user.EmailVerifiedAt, (DateTime?)now)
                    .SetProperty(user => user.EmailVerificationTokenHash, (string?)null)
                    .SetProperty(user => user.EmailVerificationExpiresAt, (DateTime?)null)
            );

        return changedRows == 1;
    }
}
