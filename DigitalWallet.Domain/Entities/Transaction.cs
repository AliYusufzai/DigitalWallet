using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public required string ReferenceNumber { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int WalletId { get; set; }
    public Wallet? Wallet { get; set; } // nullable — EF Core sets this

    public int? ReceiverWalletId { get; set; }
    public Wallet? ReceiverWallet { get; set; }
}
