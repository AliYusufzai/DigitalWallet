using DigitalWallet.Application.DTOs.Auth;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Common.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register(
        [FromBody] RegisterDto dto
    )
    {
        await _authService.RegisterAsync(dto);
        return Ok(
            ApiResponse<object>.Ok(
                "If registration can proceed, you will receive a verification email. "
                    + "If you already registered, use login or resend verification."
            )
        );
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
    {
        AuthResponseDto result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful"));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        await _authService.VerifyEmailAsync(dto);

        return Ok(ApiResponse<object>.Ok("Email verified successfully. You can now log in."));
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<ApiResponse<object>>> ResendVerification(
        [FromBody] ResendVerificationDto dto
    )
    {
        await _authService.ResendVerificationAsync(dto);

        return Ok(
            ApiResponse<object>.Ok(
                "If this account needs verification and a resend is allowed, "
                    + "a verification email will be sent."
            )
        );
    }
}
