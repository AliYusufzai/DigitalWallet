namespace DigitalWallet.Application.DTOs.Wallet;

public class TransferConfirmationResponseDto
{
    public int ConfirmationId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime ResendAvailableAtUtc { get; set; }
    public int AttemptsRemaining { get; set; }
    public int? TransactionId { get; set; }
}
