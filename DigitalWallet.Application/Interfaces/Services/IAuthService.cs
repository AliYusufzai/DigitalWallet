using DigitalWallet.Application.DTOs.Auth;

namespace DigitalWallet.Application.Interfaces.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task VerifyEmailAsync(VerifyEmailDto dto);
    Task ResendVerificationAsync(ResendVerificationDto dto);
}
