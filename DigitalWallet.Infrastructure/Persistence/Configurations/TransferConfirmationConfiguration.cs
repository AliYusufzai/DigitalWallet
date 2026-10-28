using DigitalWallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalWallet.Infrastructure.Persistence.Configurations;

public class TransferConfirmationConfiguration : IEntityTypeConfiguration<TransferConfirmation>
{
    public void Configure(EntityTypeBuilder<TransferConfirmation> builder)
    {
        builder.ToTable("TransferConfirmation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(200);
        builder.Property(x => x.OtpHash).IsRequired().HasMaxLength(256);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(x => x.SenderWalletId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(x => x.ReceiverWalletId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
