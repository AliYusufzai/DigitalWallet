using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs.Auth;

public class ResendVerificationDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}
