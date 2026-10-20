using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Services;
using DigitalWallet.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalWallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // register services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBeneficiaryService, BeneficiaryService>();

        // register JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<EmailVerificationSettings>(
            configuration.GetSection("EmailVerification")
        );

        return services;
    }
}
