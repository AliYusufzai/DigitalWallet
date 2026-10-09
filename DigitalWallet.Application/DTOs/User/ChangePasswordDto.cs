using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs.User;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Current password is required")]
    public required string CurrentPassword { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6)]
    public required string NewPassword { get; set; }

    [Required(ErrorMessage = "Confirm password is required")]
    public required string ConfirmPassword { get; set; }
}
