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
        services.AddScoped<WalletService>();
        services.AddScoped<IWalletService>(provider => provider.GetRequiredService<WalletService>());
        services.AddScoped<ITransferConfirmationService, TransferConfirmationService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBeneficiaryService, BeneficiaryService>();
        services.AddScoped<IReportService, ReportService>();

        // register JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<EmailVerificationSettings>(
            configuration.GetSection("EmailVerification")
        );
        services
            .AddOptions<WalletLimitSettings>()
            .Bind(configuration.GetSection("WalletLimits"))
            .Validate(
                settings => settings.DailyOutgoingLimit > 0,
                "Daily outgoing limit must be greater than zero."
            )
            .ValidateOnStart();

        services
            .AddOptions<TransferConfirmationSettings>()
            .Bind(configuration.GetSection("TransferConfirmation"))
            .Validate(
                settings =>
                    settings.ExpirySeconds > 0
                    && settings.MaxAttempts > 0
                    && settings.ResendCooldownSeconds > 0
                    && settings.ResendCooldownSeconds < settings.ExpirySeconds,
                "Transfer confirmation settings are invalid."
            )
            .ValidateOnStart();
        return services;
    }
}
