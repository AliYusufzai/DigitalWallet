using DigitalWallet.Application.DTOs.Reports;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<List<DailyVolumeDto>> GetDailyVolumeAsync(
        int userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<MonthlyStatementDto> GetMonthlyStatementAsync(
        int userId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    public Task<List<SpendingByTypeDto>> GetSpendingByTypeAsync(
        int userId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default
    );
}
