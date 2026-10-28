using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Application.DTOs.Wallet;

namespace DigitalWallet.Application.Interfaces.Services;

public interface ITransferConfirmationService
{
    Task<TransferConfirmationResponseDto> RequestAsync(
        int userId, TransferDto dto, CancellationToken cancellationToken = default);
    Task<TransferConfirmationResponseDto> ResendAsync(
        int userId, int confirmationId, CancellationToken cancellationToken = default);
    Task<TransactionResponseDto> ConfirmAsync(
        int userId, ConfirmTransferDto dto, CancellationToken cancellationToken = default);
}
