using System.Data;
using Dapper;
using DigitalWallet.Application.DTOs.Reports;
using DigitalWallet.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DigitalWallet.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    public async Task<List<DailyVolumeDto>> GetDailyVolumeAsync(
        int userId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(
            "dbo.GetDailyVolume",
            new { UserId = userId, StartUtc = from, EndUtc = to },
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<DailyVolumeDto>(command)).ToList();
    }

    public async Task<MonthlyStatementDto> GetMonthlyStatementAsync(
        int userId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(
            "dbo.GetMonthlyStatement",
            new { UserId = userId, StartUtc = startUtc, EndUtc = endUtc },
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);
        using var results = await connection.QueryMultipleAsync(command);
        var statement = await results.ReadSingleAsync<MonthlyStatementDto>();
        statement.Transactions = (await results.ReadAsync<StatementTransactionDto>()).ToList();
        foreach (var transaction in statement.Transactions)
            transaction.CreatedAtUtc = DateTime.SpecifyKind(transaction.CreatedAtUtc, DateTimeKind.Utc);
        return statement;
    }

    private readonly string _connectionString;

    public ReportRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The database connection string is missing");
    }

    public async Task<List<SpendingByTypeDto>> GetSpendingByTypeAsync(
        int userId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            commandText: "dbo.GetSpendingByType",
            parameters: new
            {
                UserId = userId,
                StartUtc = startUtc,
                EndUtc = endUtc,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken
        );

        var rows = await connection.QueryAsync<SpendingByTypeDto>(command);

        return rows.ToList();
    }
}
