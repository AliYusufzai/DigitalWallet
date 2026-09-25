using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Common.Wrappers;

namespace DigitalWallet.Application.Interfaces.Services;

public interface ITransactionService
{
    Task<TransactionResponseDto?> GetByIdAsync(int id);
    Task<PagedResult<TransactionResponseDto>> GetMyTransactionsAsync(
        int userId,
        int page,
        int pageSize
    );
}
