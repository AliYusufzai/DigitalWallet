using DigitalWallet.Domain.Enums;

namespace DigitalWallet.Domain.Entities;

public class Wallet
{
    public int Id { get; set; }
    public required string WalletNumber { get; set; }
    public decimal Balance { get; set; } = 0;
    public string Currency { get; set; } = "PKR";
    public WalletStatus Status { get; set; } = WalletStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User? User { get; set; } // nullable — EF Core sets this

    public List<Transaction> Transactions { get; set; } = new();
}
