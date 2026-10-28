namespace DigitalWallet.Domain.Entities;

public class TransferConfirmation
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int SenderWalletId { get; set; }
    public int ReceiverWalletId { get; set; }

    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public string? Description { get; set; }

    public required string IdempotencyKey { get; set; }

    public required string OtpHash { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime LastSentAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int? TransactionId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
