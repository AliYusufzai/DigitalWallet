using DigitalWallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalWallet.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(150);

        builder.Property(u => u.PasswordHash).IsRequired();

        builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);

        builder.Property(u => u.IsActive).HasDefaultValue(true);

        builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(u => u.Email).IsUnique();

        builder
            .HasOne(u => u.Wallet)
            .WithOne(w => w.User)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
