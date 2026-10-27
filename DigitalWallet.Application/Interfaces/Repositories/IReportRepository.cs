using DigitalWallet.Application.DTOs.Reports;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface IReportRepository
{
    public Task<List<SpendingByTypeDto>> GetSpendingByTypeAsync(
        int userId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default
    );
}
