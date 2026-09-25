namespace DigitalWallet.Application.DTOs.Wallet;

public class WalletResponseDto
{
    public int Id { get; set; }
    public required string WalletNumber { get; set; }
    public decimal Balance { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
