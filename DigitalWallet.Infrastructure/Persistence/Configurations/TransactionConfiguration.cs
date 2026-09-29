using DigitalWallet.Domain.Entities;
using DigitalWallet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalWallet.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ReferenceNumber).IsRequired().HasMaxLength(50);

        builder.Property(t => t.Amount).IsRequired().HasColumnType("decimal(18,2)");

        builder.Property(t => t.Type).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(t => t.Description).HasMaxLength(200);

        builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(t => t.ReferenceNumber).IsUnique();

        builder.HasIndex(t => t.WalletId);
        builder.HasIndex(t => t.CreatedAt);

        builder
            .HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(t => t.ReceiverWallet)
            .WithMany()
            .HasForeignKey(t => t.ReceiverWalletId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
