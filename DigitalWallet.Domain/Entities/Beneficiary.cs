namespace DigitalWallet.Domain.Entities;

public class Beneficiary
{
    public int Id { get; set; }
    public required string Nickname { get; set; }

    public required string WalletNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User? User { get; set; }
}
