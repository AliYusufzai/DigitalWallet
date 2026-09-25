using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DigitalWallet.Application.DTOs.Auth;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Settings;
using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DigitalWallet.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IUserRepository userRepository, IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        bool emailExists = await _userRepository.ExistsByEmailAsync(dto.Email);
        if (emailExists)
        {
            throw new WalletException("An account with this email already exists");
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        User user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            PhoneNumber = dto.PhoneNumber,
        };

        User createdUser = await _userRepository.CreateAsync(user);

        string token = GenerateToken(createdUser);

        // 6. return response
        return new AuthResponseDto
        {
            UserId = createdUser.Id,
            FullName = createdUser.FullName,
            Email = createdUser.Email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
        };
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

        bool passwordValid = BCrypt.Net.BCrypt.Verify(user.PasswordHash, dto.Password);
        if (!passwordValid)
        {
            throw new WalletException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new WalletException("Your account has been deactivated");
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
