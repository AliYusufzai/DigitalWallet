namespace DigitalWallet.Application.DTOs.Auth;

public class AuthResponseDto
{
    public int UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Token { get; set; } // JWT token
    public DateTime ExpiresAt { get; set; } // when token expires
}
