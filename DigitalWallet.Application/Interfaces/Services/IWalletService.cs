using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Application.DTOs.Wallet;

namespace DigitalWallet.Application.Interfaces.Services;

public interface IWalletService
{
    Task<WalletResponseDto> CreateWalletAsync(int userId);
    Task<WalletResponseDto> GetWalletAsync(int userId);
    Task<TransactionResponseDto> DepositAsync(int userId, DepositDto dto);
    Task<TransactionResponseDto> WithdrawAsync(int userId, WithdrawDto dto);
    Task<TransactionResponseDto> TransferAsync(int userId, TransferDto dto);
}
