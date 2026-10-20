using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DigitalWallet.Application.DTOs.Auth;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Settings;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DigitalWallet.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailSender _emailSender;
    private readonly EmailVerificationSettings _verificationSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IOptions<JwtSettings> jwtSettings,
        IEmailSender emailSender,
        IOptions<EmailVerificationSettings> verificationSettings,
        ILogger<AuthService> logger
    )
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
        _emailSender = emailSender;
        _verificationSettings = verificationSettings.Value;
        _logger = logger;
    }

    private static string HashToken(string token)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    private async Task SendVerificationEmailAsync(User user)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        string tokenHash = HashToken(token);

        bool canSend = await _userRepository.TrySetVerificationTokenAsync(
            user.Id,
            tokenHash,
            DateTime.UtcNow
        );

        if (!canSend)
        {
            return;
        }

        string frontendUrl = _verificationSettings.FrontendUrl.TrimEnd('/');

        string link =
            $"{frontendUrl}/verify-email"
            + $"?email={Uri.EscapeDataString(user.Email)}"
            + $"&token={Uri.EscapeDataString(token)}";

        string body =
            "Welcome to Digital Wallet!\n\n"
            + "Open this link and confirm your email address:\n\n"
            + link
            + "\n\nThis link expires in one hour."
            + "\nIf you did not request this, ignore this email.";

        try
        {
            await _emailSender.SendAsync(user.Email, "Verify your Digital Wallet email", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Verification email delivery failed for user {UserId}. Error type: {Type}",
                user.Id,
                ex.GetType().Name
            );
        }
    }

    public async Task RegisterAsync(RegisterDto dto)
    {
        string email = dto.Email.Trim();
        bool exists = await _userRepository.ExistsByEmailAsync(email);

        if (exists)
        {
            return;
        }

        var user = new User
        {
            FullName = dto.FullName,
            Email = email,
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        };

        User createdUser = await _userRepository.CreateAsync(user);

        await SendVerificationEmailAsync(createdUser);
    }

    public async Task VerifyEmailAsync(VerifyEmailDto dto)
    {
        string tokenHash = HashToken(dto.Token);

        bool verified = await _userRepository.TryVerifyEmailAsync(
            dto.Email.Trim(),
            tokenHash,
            DateTime.UtcNow
        );

        if (!verified)
        {
            throw new WalletException(
                "The verification link is invalid, expired, or already used."
            );
        }
    }

    public async Task ResendVerificationAsync(ResendVerificationDto dto)
    {
        User? user = await _userRepository.GetByEmailAsync(dto.Email.Trim());

        if (user == null || !user.IsActive || user.EmailVerifiedAt != null)
        {
            return;
        }

        await SendVerificationEmailAsync(user);
    }

    private string GenerateToken(User user)
    {
        SymmetricSecurityKey key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
        );
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials
        );

        // 5. convert token object to string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        User? user = await _userRepository.GetByEmailAsync(dto.Email);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), dto.Email);
        }

        bool passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            throw new WalletException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new WalletException("Your account has been deactivated");
        }

        if (user.EmailVerifiedAt == null)
        {
            throw new WalletException("Please verify your email before logging in.");
        }

        string token = GenerateToken(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
        };
    }
}
