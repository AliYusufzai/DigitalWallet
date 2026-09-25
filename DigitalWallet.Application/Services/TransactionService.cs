using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Common.Wrappers;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IWalletRepository _walletRepository;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IWalletRepository walletRepository
    )
    {
        _transactionRepository = transactionRepository;
        _walletRepository = walletRepository;
    }

    public async Task<TransactionResponseDto?> GetByIdAsync(int id)
    {
        Transaction? transaction = await _transactionRepository.GetByIdAsync(id);
        if (transaction == null)
        {
            throw new NotFoundException(nameof(Transaction), id);
        }

        return MapToDto(transaction);
    }

    public async Task<PagedResult<TransactionResponseDto>> GetMyTransactionsAsync(
        int userId,
        int page,
        int pageSize
    )
    {
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet == null)
        {
            throw new NotFoundException(nameof(Wallet), userId);
        }

        List<Transaction> transactions = await _transactionRepository.GetByWalletIdAsync(
            wallet.Id,
            page,
            pageSize
        );

        int totalCount = await _transactionRepository.GetTotalCountByWalletIdAsync(wallet.Id);

        return new PagedResult<TransactionResponseDto>
        {
            Items = transactions.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    private static TransactionResponseDto MapToDto(Transaction transaction)
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            ReferenceNumber = transaction.ReferenceNumber,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt,
        };
    }
}
