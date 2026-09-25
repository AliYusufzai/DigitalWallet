using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs.Auth;

public class RegisterDto
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(
        100,
        MinimumLength = 3,
        ErrorMessage = "Full name must be between 3 and 100 characters"
    )]
    public required string FullName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public required string PhoneNumber { get; set; }
}
