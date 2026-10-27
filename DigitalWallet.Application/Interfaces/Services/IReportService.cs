using DigitalWallet.Application.DTOs.Reports;

namespace DigitalWallet.Application.Interfaces.Services;

public interface IReportService
{
    Task<List<DailyVolumeDto>> GetDailyVolumeAsync(
        int userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<MonthlyStatementDto> GetMonthlyStatementAsync(
        int userId, int year, int month, CancellationToken cancellationToken = default);

    public Task<List<SpendingByTypeDto>> GetSpendingByTypeAsync(
        int userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default
    );
}
