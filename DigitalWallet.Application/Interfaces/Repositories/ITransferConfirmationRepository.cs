using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface ITransferConfirmationRepository
{
    Task LockUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastSentAtAsync(int userId, CancellationToken cancellationToken = default);
    Task<TransferConfirmation?> GetByIdAsync(int userId, int id, CancellationToken cancellationToken = default);
    Task<TransferConfirmation?> GetByIdempotencyKeyAsync(int userId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(TransferConfirmation confirmation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
