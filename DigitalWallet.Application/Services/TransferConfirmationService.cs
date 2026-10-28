using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Application.DTOs.Wallet;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Settings;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using DigitalWallet.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalWallet.Application.Services;

public class TransferConfirmationService : ITransferConfirmationService
{
    private readonly IUserRepository _users;
    private readonly IWalletRepository _wallets;
    private readonly ITransferConfirmationRepository _confirmations;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly WalletService _walletService;
    private readonly IEmailSender _emailSender;
    private readonly TransferConfirmationSettings _settings;
    private readonly ILogger<TransferConfirmationService> _logger;

    public TransferConfirmationService(
        IUserRepository users,
        IWalletRepository wallets,
        ITransferConfirmationRepository confirmations,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        WalletService walletService,
        IEmailSender emailSender,
        IOptions<TransferConfirmationSettings> settings,
        ILogger<TransferConfirmationService> logger)
    {
        _users = users;
        _wallets = wallets;
        _confirmations = confirmations;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _walletService = walletService;
        _emailSender = emailSender;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TransferConfirmationResponseDto> RequestAsync(
        int userId, TransferDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        if (dto.Amount != decimal.Round(dto.Amount, 2))
            throw new WalletException("Amount cannot have more than two decimal places.");

        TransferConfirmation confirmation;
        User user;
        Wallet receiver;
        string otp;
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _confirmations.LockUserAsync(userId, cancellationToken);
            user = await GetVerifiedUserAsync(userId);
            var sender = await _wallets.GetByUserIdAsync(userId)
                ?? throw new NotFoundException("Sender wallet not found.");
            receiver = await _wallets.GetByWalletNumberAsync(dto.ReceiverWalletNumber.Trim())
                ?? throw new NotFoundException("Receiver wallet not found.");
            var existing = await _confirmations.GetByIdempotencyKeyAsync(
                userId, dto.IdempotencyKey, cancellationToken);
            if (existing != null)
            {
                if (existing.SenderWalletId != sender.Id ||
                    existing.ReceiverWalletId != receiver.Id ||
                    existing.Amount != dto.Amount ||
                    existing.Currency != sender.Currency ||
                    existing.Description != dto.Description)
                    throw new ConflictException("This idempotency key was used for different transfer details.");
                // Replays return metadata, including completion, without another email.
                await _unitOfWork.CommitAsync();
                return MapToResponse(existing);
            }
            if (await _transactions.GetByIdempotencyKeyAsync(sender.Id, dto.IdempotencyKey) != null)
                throw new ConflictException("This idempotency key was already used for a wallet operation.");
            ValidateWallets(sender, receiver, dto.Amount, sender.Currency);
            await CheckSendCooldownAsync(userId, cancellationToken);

            otp = GenerateOtp();
            string hash = BCrypt.Net.BCrypt.HashPassword(otp);
            DateTime now = DateTime.UtcNow;
            confirmation = new TransferConfirmation
            {
                UserId = userId,
                SenderWalletId = sender.Id,
                ReceiverWalletId = receiver.Id,
                Amount = dto.Amount,
                Currency = sender.Currency,
                Description = dto.Description,
                IdempotencyKey = dto.IdempotencyKey,
                OtpHash = hash,
                CreatedAtUtc = now,
                LastSentAtUtc = now,
                ExpiresAtUtc = now.AddSeconds(_settings.ExpirySeconds)
            };
            await _confirmations.AddAsync(confirmation, cancellationToken);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        await SendOtpEmailAsync(user.Email, receiver.WalletNumber, confirmation, otp);
        return MapToResponse(confirmation);
    }

    public async Task<TransferConfirmationResponseDto> ResendAsync(
        int userId, int confirmationId, CancellationToken cancellationToken = default)
    {
        TransferConfirmation confirmation;
        User user;
        Wallet receiver;
        string otp;
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _confirmations.LockUserAsync(userId, cancellationToken);
            user = await GetVerifiedUserAsync(userId);
            confirmation = await GetConfirmationAsync(userId, confirmationId, cancellationToken);
            if (confirmation.CompletedAtUtc != null)
                throw new ConflictException("This transfer is already completed.");
            EnsureAttemptsRemaining(confirmation);
            var sender = await _wallets.GetByIdAsync(confirmation.SenderWalletId)
                ?? throw new NotFoundException("Sender wallet not found.");
            receiver = await _wallets.GetByIdAsync(confirmation.ReceiverWalletId)
                ?? throw new NotFoundException("Receiver wallet not found.");
            if (sender.UserId != userId)
                throw new NotFoundException("Confirmation request not found.");
            ValidateWallets(sender, receiver, confirmation.Amount, confirmation.Currency);
            await CheckSendCooldownAsync(userId, cancellationToken);

            otp = GenerateOtp();
            // Ensure a resend really invalidates the previous numeric code.
            while (BCrypt.Net.BCrypt.Verify(otp, confirmation.OtpHash))
                otp = GenerateOtp();
            confirmation.OtpHash = BCrypt.Net.BCrypt.HashPassword(otp);
            confirmation.LastSentAtUtc = DateTime.UtcNow;
            confirmation.ExpiresAtUtc = confirmation.LastSentAtUtc.AddSeconds(_settings.ExpirySeconds);
            // FailedAttempts deliberately survives resends.
            await _confirmations.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        await SendOtpEmailAsync(user.Email, receiver.WalletNumber, confirmation, otp);
        return MapToResponse(confirmation);
    }

    public async Task<TransactionResponseDto> ConfirmAsync(
        int userId, ConfirmTransferDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _confirmations.LockUserAsync(userId, cancellationToken);
            await GetVerifiedUserAsync(userId);
            var confirmation = await GetConfirmationAsync(userId, dto.ConfirmationId, cancellationToken);
            if (confirmation.CompletedAtUtc != null)
            {
                var original = await _transactions.GetByIdAsync(
                    confirmation.TransactionId!.Value, userId)
                    ?? throw new NotFoundException("Completed transaction not found.");
                var recipient = await _wallets.GetByIdAsync(confirmation.ReceiverWalletId);
                await _unitOfWork.CommitAsync();
                return WalletService.MapToTransactionDto(original, recipient?.WalletNumber);
            }
            EnsureAttemptsRemaining(confirmation);
            if (DateTime.UtcNow >= confirmation.ExpiresAtUtc)
                throw new WalletException("The confirmation code has expired. Request a new code.");
            if (!BCrypt.Net.BCrypt.Verify(dto.Otp, confirmation.OtpHash))
            {
                confirmation.FailedAttempts++;
                await _confirmations.SaveChangesAsync(cancellationToken);
                // Commit the failure count before returning the validation error.
                await _unitOfWork.CommitAsync();
                throw new WalletException(confirmation.FailedAttempts >= _settings.MaxAttempts
                    ? "This confirmation request has reached its attempt limit."
                    : "The confirmation code is incorrect.");
            }
            if (DateTime.UtcNow >= confirmation.ExpiresAtUtc)
                throw new WalletException("The confirmation code has expired. Request a new code.");
            var sender = await _wallets.GetByIdAsync(confirmation.SenderWalletId)
                ?? throw new NotFoundException("Sender wallet not found.");
            var receiver = await _wallets.GetByIdAsync(confirmation.ReceiverWalletId)
                ?? throw new NotFoundException("Receiver wallet not found.");
            if (sender.UserId != userId)
                throw new NotFoundException("Confirmation request not found.");
            ValidateWallets(sender, receiver, confirmation.Amount, confirmation.Currency);
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _walletService.TransferConfirmedAsync(confirmation, receiver.WalletNumber);
            confirmation.TransactionId = result.Id;
            confirmation.CompletedAtUtc = DateTime.UtcNow;
            confirmation.OtpHash = string.Empty;
            await _confirmations.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync();
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<User> GetVerifiedUserAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");
        if (!user.IsActive || user.EmailVerifiedAt == null)
            throw new WalletException("An active account with a verified email is required.");
        return user;
    }

    private async Task<TransferConfirmation> GetConfirmationAsync(
        int userId, int id, CancellationToken cancellationToken) =>
        await _confirmations.GetByIdAsync(userId, id, cancellationToken)
            ?? throw new NotFoundException("Confirmation request not found.");

    private void EnsureAttemptsRemaining(TransferConfirmation confirmation)
    {
        if (confirmation.FailedAttempts >= _settings.MaxAttempts)
            throw new WalletException("This confirmation request has reached its attempt limit.");
    }

    private async Task CheckSendCooldownAsync(int userId, CancellationToken cancellationToken)
    {
        var lastSent = await _confirmations.GetLastSentAtAsync(userId, cancellationToken);
        if (lastSent == null)
            return;
        var remaining = lastSent.Value.AddSeconds(_settings.ResendCooldownSeconds) - DateTime.UtcNow;
        if (remaining > TimeSpan.Zero)
            throw new TooManyRequestsException((int)Math.Ceiling(remaining.TotalSeconds));
    }

    private static void ValidateWallets(Wallet sender, Wallet receiver, decimal amount, string currency)
    {
        if (sender.Id == receiver.Id)
            throw new WalletException("You cannot transfer to your own wallet.");
        if (sender.Status != WalletStatus.Active || receiver.Status != WalletStatus.Active)
            throw new WalletException("Both wallets must be active.");
        if (sender.Currency != currency || receiver.Currency != currency)
            throw new WalletException("Wallet currencies must match the requested currency.");
        if (sender.Balance < amount)
            throw new InsufficientFundsException(sender.Balance, amount);
    }

    private static void Validate(object dto)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
            throw new WalletException(errors[0].ErrorMessage ?? "Invalid request.");
    }

    private static string GenerateOtp() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    private async Task SendOtpEmailAsync(
        string email, string receiverWalletNumber, TransferConfirmation confirmation, string otp)
    {
        string body = $"""
            You requested a transfer.

            Recipient wallet: {receiverWalletNumber}
            Amount: {confirmation.Currency} {confirmation.Amount.ToString("N2", CultureInfo.InvariantCulture)}

            Your confirmation code is: {otp}
            This code expires at {confirmation.ExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC,
            {_settings.ExpirySeconds} seconds after generation.
            Do not share this code with anyone.
            If you did not request this transfer, do not confirm it.
            """;
        try
        {
            await _emailSender.SendAsync(email, "Confirm your Sikka transfer", body);
        }
        catch (Exception)
        {
            // SMTP exceptions can contain message or recipient data.
            _logger.LogWarning("Email delivery could not be confirmed for transfer request {ConfirmationId}",
                confirmation.Id);
            throw new EmailDeliveryException();
        }
    }

    private TransferConfirmationResponseDto MapToResponse(TransferConfirmation confirmation) => new()
    {
        ConfirmationId = confirmation.Id,
        ExpiresAtUtc = DateTime.SpecifyKind(confirmation.ExpiresAtUtc, DateTimeKind.Utc),
        ResendAvailableAtUtc = DateTime.SpecifyKind(
            confirmation.LastSentAtUtc.AddSeconds(_settings.ResendCooldownSeconds), DateTimeKind.Utc),
        AttemptsRemaining = Math.Max(0, _settings.MaxAttempts - confirmation.FailedAttempts),
        TransactionId = confirmation.TransactionId
    };
}
