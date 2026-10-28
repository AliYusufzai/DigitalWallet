using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs.Wallet;

public class ConfirmTransferDto
{
    [Range(1, int.MaxValue)]
    public int ConfirmationId { get; set; }

    [Required]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "OTP must contain exactly 6 digits.")]
    public string Otp { get; set; } = string.Empty;
}
