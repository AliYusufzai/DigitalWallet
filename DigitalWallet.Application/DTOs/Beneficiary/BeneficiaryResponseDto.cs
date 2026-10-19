namespace DigitalWallet.Application.DTOs.Beneficiary;

public class BeneficiaryResponseDto
{
    public int Id { get; set; }

    public required string Nickname { get; set; }

    public required string WalletNumber { get; set; }

    public DateTime CreatedAt { get; set; }
}
