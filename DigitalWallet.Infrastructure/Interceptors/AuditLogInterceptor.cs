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

        var walletEntries = context
            .ChangeTracker.Entries<Wallet>()
            .Where(entry => entry.State == EntityState.Modified)
            .ToList();

        var timestamp = DateTime.UtcNow;

        foreach (var entry in walletEntries)
        {
            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            string[] auditedProperties = { nameof(Wallet.Balance), nameof(Wallet.Status) };

            foreach (string propertyName in auditedProperties)
            {
                var property = entry.Property(propertyName);

                if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))
                {
                    continue;
                }

                oldValues[propertyName] = property.OriginalValue;
                newValues[propertyName] = property.CurrentValue;
            }
            if (newValues.Count == 0)
            {
                continue;
            }

            var auditLog = new AuditLog
            {
                ActorUserId = _currentUser.UserId,
                EntityName = nameof(Wallet),
                EntityId = entry.Entity.Id.ToString(),
                Action = "Updated",
                OldValues = JsonSerializer.Serialize(oldValues),
                NewValues = JsonSerializer.Serialize(newValues),
                CreatedAtUtc = timestamp,
            };

            context.Set<AuditLog>().Add(auditLog);
        }
    }
}
