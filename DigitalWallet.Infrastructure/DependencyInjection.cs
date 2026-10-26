using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Application.Settings;
using DigitalWallet.Infrastructure.Interceptors;
using DigitalWallet.Infrastructure.Persistence;
using DigitalWallet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<AuditLogInterceptor>();
        services.AddDbContext<AppDbContext>(
            (serviceProvider, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                options.AddInterceptors(serviceProvider.GetRequiredService<AuditLogInterceptor>());
            }
        );

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();

        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
