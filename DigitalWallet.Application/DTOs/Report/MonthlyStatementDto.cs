namespace DigitalWallet.Application.DTOs.Reports;

public class MonthlyStatementDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalIncoming { get; set; }
    public decimal TotalOutgoing { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<StatementTransactionDto> Transactions { get; set; } = [];
}

public class StatementTransactionDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal BalanceAfter { get; set; }
}
