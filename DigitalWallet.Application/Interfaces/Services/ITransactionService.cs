using DigitalWallet.Application.DTOs;
using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Common.Wrappers;

namespace DigitalWallet.Application.Interfaces.Services;

public interface ITransactionService
{
    Task<TransactionResponseDto?> GetByIdAsync(int id, int userId);
    Task<PagedResult<TransactionResponseDto>> GetMyTransactionsAsync(
        int userId,
        TransactionFilterDto filter
    );
}
