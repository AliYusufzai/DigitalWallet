namespace DigitalWallet.Application.DTOs.Transaction;

public class TransactionResponseDto
{
    public int Id { get; set; }
    public required string ReferenceNumber { get; set; }
    public decimal Amount { get; set; }
    public required string Type { get; set; } // "Deposit", "Withdrawal", "Transfer"
    public required string Status { get; set; } // "Pending", "Completed", "Failed"
    public string? Description { get; set; }
    public string? ReceiverWalletNumber { get; set; } // only for transfers
    public DateTime CreatedAt { get; set; }
}