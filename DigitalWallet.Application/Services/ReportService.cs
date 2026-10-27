using DigitalWallet.Application.DTOs.Reports;
using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Application.Interfaces.Services;
using DigitalWallet.Domain.Exceptions;

namespace DigitalWallet.Application.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public Task<List<SpendingByTypeDto>> GetSpendingByTypeAsync(
        int userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default
    )
    {
        var (startUtc, endUtc) = GetUtcRange(userId, from, to);
        return _reportRepository.GetSpendingByTypeAsync(userId, startUtc, endUtc, cancellationToken);
    }

    public Task<List<DailyVolumeDto>> GetDailyVolumeAsync(
        int userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var (startUtc, endUtc) = GetUtcRange(userId, from, to);
        return _reportRepository.GetDailyVolumeAsync(userId, startUtc, endUtc, cancellationToken);
    }

    public async Task<MonthlyStatementDto> GetMonthlyStatementAsync(
        int userId, int year, int month, CancellationToken cancellationToken = default)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12)
            throw new WalletException("A valid year and month are required.");

        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var (startUtc, endUtc) = GetUtcRange(userId, from, to);
        var statement = await _reportRepository.GetMonthlyStatementAsync(
            userId, startUtc, endUtc, cancellationToken);
        statement.Year = year;
        statement.Month = month;
        return statement;
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetUtcRange(
        int userId, DateOnly from, DateOnly to)
    {
        if (userId <= 0)
        {
            throw new WalletException("A valid User Id is required");
        }

        if (from > to)
        {
            throw new WalletException("the start date must not be after the end date");
        }

        if (from == DateOnly.MinValue || to == DateOnly.MaxValue)
        {
            throw new WalletException("The requested dates are outside the supported range.");
        }

        var pakistanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");

        DateTime startLocal = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        DateTime endLocal = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        DateTime startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, pakistanTimeZone);
        DateTime endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, pakistanTimeZone);

        return (startUtc, endUtc);
    }
}
