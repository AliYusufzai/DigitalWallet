using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Exceptions;
using DigitalWallet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DigitalWallet.Infrastructure.Repositories;

public class TransferConfirmationRepository : ITransferConfirmationRepository
{
    private readonly AppDbContext _context;

    public TransferConfirmationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task LockUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("OTP operations require an active transaction.");
        await using var command = _context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 5000;
            SELECT @result;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = $"transfer-confirmation:{userId}";
        command.Parameters.Add(parameter);
        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
            throw new ConflictException("Another confirmation operation is in progress. Retry the same request.");
    }

    public Task<DateTime?> GetLastSentAtAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.TransferConfirmations.Where(x => x.UserId == userId)
            .MaxAsync(x => (DateTime?)x.LastSentAtUtc, cancellationToken);

    public Task<TransferConfirmation?> GetByIdAsync(int userId, int id, CancellationToken cancellationToken = default)
    {
        return _context.TransferConfirmations.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == userId, cancellationToken
        );
    }

    public Task<TransferConfirmation?> GetByIdempotencyKeyAsync(int userId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _context.TransferConfirmations.FirstOrDefaultAsync(x =>
            x.UserId == userId && x.IdempotencyKey == idempotencyKey, cancellationToken
        );
    }

    public async Task AddAsync(TransferConfirmation confirmation, CancellationToken cancellationToken = default)
    {
        _context.TransferConfirmations.Add(confirmation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
