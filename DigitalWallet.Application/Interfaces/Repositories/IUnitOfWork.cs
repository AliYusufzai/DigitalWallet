namespace DigitalWallet.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
