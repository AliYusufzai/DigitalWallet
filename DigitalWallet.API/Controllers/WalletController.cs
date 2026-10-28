using System.Security.Claims;
using DigitalWallet.Application.DTOs.Transaction;
using DigitalWallet.Application.DTOs.Wallet;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Common.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.API.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ITransferConfirmationService _confirmationService;

    public WalletController(IWalletService walletService, ITransferConfirmationService confirmationService)
    {
        _walletService = walletService;
        _confirmationService = confirmationService;
    }

    private int GetUserId()
    {
        string? userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return int.Parse(userId);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WalletResponseDto>>> CreateWallet()
    {
        int userId = GetUserId();
        WalletResponseDto result = await _walletService.CreateWalletAsync(userId);
        return Ok(ApiResponse<WalletResponseDto>.Ok(result, "Wallet created successfully"));
    }

    // GET api/wallet
    [HttpGet]
    public async Task<ActionResult<ApiResponse<WalletResponseDto>>> GetWallet()
    {
        int userId = GetUserId();
        WalletResponseDto result = await _walletService.GetWalletAsync(userId);
        return Ok(ApiResponse<WalletResponseDto>.Ok(result));
    }

    // POST api/wallet/deposit
    [HttpPost("deposit")]
    public async Task<ActionResult<ApiResponse<TransactionResponseDto>>> Deposit(
        [FromBody] DepositDto dto
    )
    {
        int userId = GetUserId();
        TransactionResponseDto result = await _walletService.DepositAsync(userId, dto);
        return Ok(ApiResponse<TransactionResponseDto>.Ok(result, "Deposit successful"));
    }

    // POST api/wallet/withdraw
    [HttpPost("withdraw")]
    public async Task<ActionResult<ApiResponse<TransactionResponseDto>>> Withdraw(
        [FromBody] WithdrawDto dto
    )
    {
        int userId = GetUserId();
        TransactionResponseDto result = await _walletService.WithdrawAsync(userId, dto);
        return Ok(ApiResponse<TransactionResponseDto>.Ok(result, "Withdrawal successful"));
    }

    // POST api/wallet/transfer
    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<TransferConfirmationResponseDto>>> Transfer(
        [FromBody] TransferDto dto,
        CancellationToken cancellationToken
    )
    {
        int userId = GetUserId();
        var result = await _confirmationService.RequestAsync(userId, dto, cancellationToken);
        return Ok(ApiResponse<TransferConfirmationResponseDto>.Ok(result, "Confirmation request retrieved"));
    }

    [HttpPost("transfer/confirm")]
    public async Task<ActionResult<ApiResponse<TransactionResponseDto>>> ConfirmTransfer(
        [FromBody] ConfirmTransferDto dto, CancellationToken cancellationToken)
    {
        var result = await _confirmationService.ConfirmAsync(GetUserId(), dto, cancellationToken);
        return Ok(ApiResponse<TransactionResponseDto>.Ok(result, "Transfer successful"));
    }

    [HttpPost("transfer/confirmations/{confirmationId:int:min(1)}/resend")]
    public async Task<ActionResult<ApiResponse<TransferConfirmationResponseDto>>> ResendTransferCode(
        int confirmationId, CancellationToken cancellationToken)
    {
        var result = await _confirmationService.ResendAsync(GetUserId(), confirmationId, cancellationToken);
        return Ok(ApiResponse<TransferConfirmationResponseDto>.Ok(result, "New confirmation code sent"));
    }
}
