using System.ComponentModel.DataAnnotations;

namespace DigitalWallet.Application.DTOs.User;

public class UpdateProfileDto
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 3)]
    public required string FullName { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid Phone number")]
    public required string PhoneNumber { get; set; }
}
