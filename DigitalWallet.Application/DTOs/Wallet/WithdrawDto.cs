using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs.Wallet;

public class WithdrawDto
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(1, 1000000, ErrorMessage = "Amount must be between 1 and 1,000,000")]
    public decimal Amount { get; set; }

    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string? Description { get; set; }
}
