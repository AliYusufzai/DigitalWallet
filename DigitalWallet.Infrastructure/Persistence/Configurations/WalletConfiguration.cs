using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalWallet.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.WalletNumber).IsRequired().HasMaxLength(30);

        builder
            .Property(u => u.Balance)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(u => u.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("PKR");

        builder
            .Property(u => u.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(WalletStatus.Active);

        builder.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(w => w.WalletNumber).IsUnique();

        builder.HasIndex(w => w.UserId);

        builder
            .HasMany(w => w.Transactions)
            .WithOne(t => t.Wallet)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
