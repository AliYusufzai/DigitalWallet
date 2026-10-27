using DigitalWallet.Application.DTOs.Reports;

namespace DigitalWallet.Application.Interfaces.Services;

public interface IReportService
{
    public Task<List<SpendingByTypeDto>> GetSpendingByTypeAsync(
        int userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default
    );
}
