using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs.Auth;

public class VerifyEmailDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(64, MinimumLength = 64)]
    public required string Token { get; set; }
}
