using System.Text.Json;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigitalWallet.Infrastructure.Interceptors;

public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;

    public AuditLogInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        PrepareAuditEntries(eventData.Context);

        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        PrepareAuditEntries(eventData.Context);

        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void PrepareAuditEntries(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();

        var entries = context
            .ChangeTracker.Entries()
            .Where(entry =>
                (
                    entry.State == EntityState.Modified
                    && (entry.Entity is Wallet || entry.Entity is User)
                ) || (entry.State == EntityState.Deleted && entry.Entity is Beneficiary)
            )
            .ToList();

        var timestamp = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            string entityName;
            string entityId;
            string[] auditedProperties;

            if (entry.Entity is Wallet wallet)
            {
                entityName = nameof(Wallet);
                entityId = wallet.Id.ToString();

                auditedProperties = new[] { nameof(Wallet.Balance), nameof(Wallet.Status) };
            }
            else if (entry.Entity is User user)
            {
                entityName = nameof(User);
                entityId = user.Id.ToString();

                auditedProperties = new[] { nameof(User.FullName), nameof(User.PhoneNumber) };
            }
            else if (entry.Entity is Beneficiary beneficiary)
            {
                entityName = nameof(Beneficiary);
                entityId = beneficiary.Id.ToString();

                auditedProperties = new[]
                {
                    nameof(Beneficiary.Nickname),
                    nameof(Beneficiary.WalletNumber),
                    nameof(Beneficiary.UserId),
                };
            }
            else
            {
                continue;
            }

            bool isDeleted = entry.State == EntityState.Deleted;

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (string propertyName in auditedProperties)
            {
                var property = entry.Property(propertyName);

                if (isDeleted)
                {
                    oldValues[propertyName] = property.OriginalValue;
                    continue;
                }

                if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))
                {
                    continue;
                }

                oldValues[propertyName] = property.OriginalValue;
                newValues[propertyName] = property.CurrentValue;
            }
            if (!isDeleted && newValues.Count == 0)
            {
                continue;
            }

            var auditLog = new AuditLog
            {
                ActorUserId = _currentUser.UserId,
                EntityName = entityName,
                EntityId = entityId,
                Action = isDeleted ? "Deleted" : "Updated",
                OldValues = JsonSerializer.Serialize(oldValues),
                NewValues = isDeleted ? null : JsonSerializer.Serialize(newValues),
                CreatedAtUtc = timestamp,
            };

            context.Set<AuditLog>().Add(auditLog);
        }
    }
}
