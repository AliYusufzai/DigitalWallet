namespace DigitalWallet.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Wallet? Wallet { get; set; } // nullable — EF Core sets this
}
