using DigitalWallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalWallet.Infrastructure.Persistence.Configurations;

public class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> builder)
    {
        builder.ToTable("Beneficiaries");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Nickname).IsRequired().HasMaxLength(50);

        builder.Property(b => b.WalletNumber).IsRequired().HasMaxLength(20);

        builder.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder
            .HasOne(b => b.User)
            .WithMany(u => u.Beneficiaries)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.UserId, b.WalletNumber }).IsUnique();
    }
}
