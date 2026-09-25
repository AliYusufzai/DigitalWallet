using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Application.DTOs.Wallet;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public WalletService(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository
    )
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<WalletResponseDto> CreateWalletAsync(int userId)
    {
        // 1. check user doesn't already have a wallet
        bool walletExists = await _walletRepository.ExistsByUserIdAsync(userId);
        if (walletExists)
        {
            throw new WalletException("User already has a wallet");
        }

        // 2. generate unique wallet number
        string walletNumber = GenerateWalletNumber();

        // 3. create wallet entity
        Wallet wallet = new Wallet
        {
            WalletNumber = walletNumber,
            UserId = userId,
            Balance = 0,
            Currency = "PKR",
            Status = WalletStatus.Active,
        };

        // 4. save to database
        Wallet created = await _walletRepository.CreateAsync(wallet);

        // 5. return response
        return MapToWalletDto(created);
    }

    public async Task<WalletResponseDto> GetWalletAsync(int userId)
    {
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId);

        if (wallet == null)
        {
            throw new NotFoundException(nameof(Wallet), userId);
        }

        return MapToWalletDto(wallet);
    }

    public async Task<TransactionResponseDto> DepositAsync(int userId, DepositDto dto)
    {
        // 1. get wallet
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet == null)
        {
            throw new NotFoundException(nameof(Wallet), userId);
        }

        // 2. check wallet is active
        if (wallet.Status != WalletStatus.Active)
        {
            throw new WalletException("Wallet is not active");
        }

        // 3. add money to balance
        wallet.Balance += dto.Amount;

        // 4. update wallet in database
        await _walletRepository.UpdateAsync(wallet);

        // 5. create transaction record
        Transaction transaction = new Transaction
        {
            ReferenceNumber = GenerateReferenceNumber(),
            WalletId = wallet.Id,
            Amount = dto.Amount,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed,
            Description = dto.Description ?? "Deposit",
        };

        Transaction created = await _transactionRepository.CreateAsync(transaction);

        // 6. return transaction response
        return MapToTransactionDto(created);
    }

    public async Task<TransactionResponseDto> WithdrawAsync(int userId, WithdrawDto dto)
    {
        Wallet? wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet == null)
        {
            throw new NotFoundException(nameof(Wallet), userId);
        }

        if (wallet.Status != WalletStatus.Active)
        {
            throw new WalletException("Wallet is not active");
        }

        if (dto.Amount > wallet.Balance)
        {
            throw new InsufficientFundsException(wallet.Balance, dto.Amount);
        }

        wallet.Balance -= dto.Amount;
        await _walletRepository.UpdateAsync(wallet);

        Transaction transaction = new Transaction
        {
            ReferenceNumber = GenerateReferenceNumber(),
            WalletId = wallet.Id,
            Amount = dto.Amount,
            Type = TransactionType.Withdrawal,
            Status = TransactionStatus.Completed,
            Description = dto.Description ?? "Withdrawal",
        };

        Transaction created = await _transactionRepository.CreateAsync(transaction);
        return MapToTransactionDto(created);
    }

    public async Task<TransactionResponseDto> TransferAsync(int userId, TransferDto dto)
    {
        Wallet? senderWallet = await _walletRepository.GetByUserIdAsync(userId);
        if (senderWallet == null)
        {
            throw new NotFoundException(nameof(Wallet), userId);
        }

        if (senderWallet.Status != WalletStatus.Active)
        {
            throw new WalletException("Your wallet is not active");
        }

        Wallet? receiverWallet = await _walletRepository.GetByWalletNumberAsync(
            dto.ReceiverWalletNumber
        );
        if (receiverWallet == null)
        {
            throw new NotFoundException(nameof(Wallet), dto.ReceiverWalletNumber);
        }

        if (senderWallet.Id == receiverWallet.Id)
        {
            throw new WalletException("Cannot transfer to your own wallet");
        }

        if (receiverWallet.Status != WalletStatus.Active)
        {
            throw new WalletException("Receiver wallet is not active");
        }

        if (dto.Amount > senderWallet.Balance)
        {
            throw new InsufficientFundsException(senderWallet.Balance, dto.Amount);
        }

        senderWallet.Balance -= dto.Amount;
        await _walletRepository.UpdateAsync(senderWallet);

        receiverWallet.Balance += dto.Amount;
        await _walletRepository.UpdateAsync(receiverWallet);

        Transaction transaction = new Transaction
        {
            ReferenceNumber = GenerateReferenceNumber(),
            WalletId = senderWallet.Id,
            ReceiverWalletId = receiverWallet.Id,
            Amount = dto.Amount,
            Type = TransactionType.Transfer,
            Status = TransactionStatus.Completed,
            Description = dto.Description ?? $"Transfer to {dto.ReceiverWalletNumber}",
        };

        Transaction created = await _transactionRepository.CreateAsync(transaction);
        return MapToTransactionDto(created, dto.ReceiverWalletNumber);
    }

    private static string GenerateWalletNumber()
    {
        return $"W-{new Random().Next(10000000, 99999999)}";
    }

    private static string GenerateReferenceNumber()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
    }

    private static WalletResponseDto MapToWalletDto(Wallet wallet)
    {
        return new WalletResponseDto
        {
            Id = wallet.Id,
            WalletNumber = wallet.WalletNumber,
            Balance = wallet.Balance,
            Currency = wallet.Currency,
            Status = wallet.Status.ToString(),
            CreatedAt = wallet.CreatedAt,
        };
    }

    private static TransactionResponseDto MapToTransactionDto(
        Transaction transaction,
        string? receiverWalletNumber = null
    )
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            ReferenceNumber = transaction.ReferenceNumber,
            Amount = transaction.Amount,
            Type = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            Description = transaction.Description,
            ReceiverWalletNumber = receiverWalletNumber,
            CreatedAt = transaction.CreatedAt,
        };
    }
}
