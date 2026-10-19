using DigitalWallet.Application.DTOs;
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

    public async Task<TransactionResponseDto?> GetByIdAsync(int id, int userId)
    {
        Transaction? transaction = await _transactionRepository.GetByIdAsync(id, userId);
        if (transaction == null)
        {
            throw new NotFoundException(nameof(Transaction), id);
        }

        return MapToDto(transaction);
    }

    public async Task<PagedResult<TransactionResponseDto>> GetMyTransactionsAsync(
        int userId,
        TransactionFilterDto filter
    )
    {
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet == null)
        {
            throw new NotFoundException(nameof(Wallet), userId);
        }

        List<Transaction> transactions = await _transactionRepository.GetByWalletIdAsync(
            wallet.Id,
            filter
        );

        int totalCount = await _transactionRepository.GetTotalCountByWalletIdAsync(
            wallet.Id,
            filter
        );

        return new PagedResult<TransactionResponseDto>
        {
            Items = transactions.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    private static TransactionResponseDto MapToDto(Transaction transaction)
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            Direction = transaction.Direction.ToString(),
            ReferenceNumber = transaction.ReferenceNumber,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt,
        };
    }
}
