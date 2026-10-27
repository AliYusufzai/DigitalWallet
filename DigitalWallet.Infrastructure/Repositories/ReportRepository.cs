using System.Data;
using Dapper;
using DigitalWallet.Application.DTOs.Reports;
using DigitalWallet.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DigitalWallet.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
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
