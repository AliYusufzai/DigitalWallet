using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Application.DTOs.Wallet;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Settings;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace DigitalWallet.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly WalletLimitSettings _walletLimits;

    public WalletService(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        IOptions<WalletLimitSettings> walletLimits
    )
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _walletLimits = walletLimits.Value;
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

    public Task<TransactionResponseDto> DepositAsync(int userId, DepositDto dto) =>
        ExecuteAsync(
            userId,
            dto.Amount,
            dto.Description,
            dto.IdempotencyKey,
            TransactionType.Deposit
        );

    public Task<TransactionResponseDto> WithdrawAsync(int userId, WithdrawDto dto) =>
        ExecuteAsync(
            userId,
            dto.Amount,
            dto.Description,
            dto.IdempotencyKey,
            TransactionType.Withdrawal
        );

    public Task<TransactionResponseDto> TransferAsync(int userId, TransferDto dto) =>
        ExecuteAsync(
            userId,
            dto.Amount,
            dto.Description,
            dto.IdempotencyKey,
            TransactionType.Transfer,
            dto.ReceiverWalletNumber
        );

    private async Task<TransactionResponseDto> ExecuteAsync(
        int userId,
        decimal amount,
        string? description,
        string key,
        TransactionType type,
        string? receiverNumber = null
    )
    {
        if (amount < 1 || amount > 1_000_000 || decimal.Round(amount, 2) != amount)
            throw new WalletException(
                "Amount must be between 1 and 1,000,000 with at most two decimal places."
            );
        if (string.IsNullOrWhiteSpace(key) || key.Length > 64)
            throw new WalletException("An idempotency key of 1 to 64 characters is required.");
        if (description?.Length > 200)
            throw new WalletException("Description cannot exceed 200 characters.");
        if (type == TransactionType.Transfer && string.IsNullOrWhiteSpace(receiverNumber))
            throw new WalletException("Receiver wallet number is required.");

        var wallet =
            await _walletRepository.GetByUserIdAsync(userId)
            ?? throw new NotFoundException(nameof(Wallet), userId);
        // Hash the original request, so reusing a key with different inputs is rejected.
        string requestHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                    new
                    {
                        Type = type,
                        Amount = amount.ToString(
                            "G29",
                            System.Globalization.CultureInfo.InvariantCulture
                        ),
                        Description = description,
                        Receiver = receiverNumber,
                    }
                )
            )
        );
        var existing = await _transactionRepository.GetByIdempotencyKeyAsync(wallet.Id, key);
        if (existing != null)
        {
            if (existing.RequestHash != requestHash)
                throw new ConflictException(
                    "This idempotency key was already used for a different request."
                );
            return MapToTransactionDto(existing, existing.ReceiverWallet?.WalletNumber);
        }

        if (wallet.Status != WalletStatus.Active)
            throw new WalletException("Wallet is not active.");
        Wallet? receiver = null;
        if (type == TransactionType.Transfer)
        {
            receiver =
                await _walletRepository.GetByWalletNumberAsync(receiverNumber!)
                ?? throw new NotFoundException(nameof(Wallet), receiverNumber!);
            if (receiver.Id == wallet.Id)
                throw new WalletException("Cannot transfer to your own wallet.");
            if (receiver.Status != WalletStatus.Active)
                throw new WalletException("Receiver wallet is not active.");
            if (receiver.Currency != wallet.Currency)
                throw new WalletException("Wallet currencies must match.");
        }
        if (type != TransactionType.Deposit && amount > wallet.Balance)
            throw new InsufficientFundsException(wallet.Balance, amount);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var operationTimeUtc = DateTime.UtcNow;
            if (type == TransactionType.Withdrawal || type == TransactionType.Transfer)
            {
                var dayRange = GetPakistanDayUtcRange(operationTimeUtc);

                decimal spentToday = await _transactionRepository.GetOutgoingTotalAsync(
                    wallet.Id,
                    dayRange.StartUtc,
                    dayRange.EndUtc
                );

                decimal dailyLimit = _walletLimits.DailyOutgoingLimit;
                if (spentToday + amount > dailyLimit)
                {
                    throw new WalletException(
                        $"Daily outgoing limit of PKR {dailyLimit:N2} exceeded."
                    );
                }
            }
            wallet.Balance += type == TransactionType.Deposit ? amount : -amount;
            await _walletRepository.UpdateAsync(wallet);
            if (receiver != null)
            {
                receiver.Balance += amount;
                await _walletRepository.UpdateAsync(receiver);
            }
            var transaction = new Transaction
            {
                ReferenceNumber = GenerateReferenceNumber(),
                WalletId = wallet.Id,
                ReceiverWalletId = receiver?.Id,
                Amount = amount,
                Type = type,
                Direction =
                    type == TransactionType.Deposit
                        ? TransactionDirection.Incoming
                        : TransactionDirection.Outgoing,
                Status = TransactionStatus.Completed,
                Description =
                    description
                    ?? (
                        receiver != null ? $"Transfer to {receiver.WalletNumber}" : type.ToString()
                    ),
                IdempotencyKey = key,
                RequestHash = requestHash,
                CreatedAt = operationTimeUtc,
            };
            await _transactionRepository.CreateAsync(transaction);
            if (receiver != null)
            {
                await _transactionRepository.CreateAsync(
                    new Transaction
                    {
                        ReferenceNumber = transaction.ReferenceNumber,
                        WalletId = receiver.Id,
                        ReceiverWalletId = wallet.Id,
                        Amount = amount,
                        Type = TransactionType.Transfer,
                        Direction = TransactionDirection.Incoming,
                        Status = TransactionStatus.Completed,
                        Description = $"Transfer from {wallet.WalletNumber}",
                        CreatedAt = operationTimeUtc,
                    }
                );
            }
            await _unitOfWork.CommitAsync();
            return MapToTransactionDto(transaction, receiver?.WalletNumber);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetPakistanDayUtcRange(DateTime utcNow)
    {
        var pakistanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");

        var pakistanNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, pakistanTimeZone);

        var startLocal = DateTime.SpecifyKind(pakistanNow.Date, DateTimeKind.Unspecified);

        var endLocal = startLocal.AddDays(1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(startLocal, pakistanTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(endLocal, pakistanTimeZone)
        );
    }

    private static string GenerateWalletNumber()
    {
        return $"W-{new Random().Next(10000000, 99999999)}";
    }

    private static string GenerateReferenceNumber()
    {
        return $"TXN-{Guid.NewGuid():N}";
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
            Direction = transaction.Direction.ToString(),
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
