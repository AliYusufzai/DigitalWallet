using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Services;

public interface IAuditWriter
{
    Task WriteBeneficiaryCreatedAsync(Beneficiary beneficiary);
}
