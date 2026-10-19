using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs.Beneficiary;

public class AddBeneficiaryDto
{
    [Required(ErrorMessage = "Nickname is required")]
    [MaxLength(50, ErrorMessage = "Nickname cannot exceed 50 characters")]
    public required string Nickname { get; set; }

    [Required(ErrorMessage = "Wallet number is required")]
    public required string WalletNumber { get; set; }
}
