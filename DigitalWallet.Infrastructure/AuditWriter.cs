using System.Text.Json;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Infrastructure.Persistence;

namespace DigitalWallet.Infrastructure;

public class AuditWriter : IAuditWriter
{
    private readonly AppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AuditWriter(AppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task WriteBeneficiaryCreatedAsync(Beneficiary beneficiary)
    {
        // The caller owns the transaction covering the entity insert and this audit save.
        if (_context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Creation auditing requires an active transaction.");
        if (beneficiary.Id <= 0)
            throw new InvalidOperationException("Save the beneficiary before recording its creation.");

        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = _currentUser.UserId,
            EntityName = nameof(Beneficiary),
            EntityId = beneficiary.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Action = "Created",
            OldValues = null,
            NewValues = JsonSerializer.Serialize(new
            {
                beneficiary.Nickname,
                beneficiary.WalletNumber,
                beneficiary.UserId
            }),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
