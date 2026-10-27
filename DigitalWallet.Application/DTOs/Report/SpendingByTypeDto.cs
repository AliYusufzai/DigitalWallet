namespace DigitalWallet.Application.DTOs.Reports;

public class SpendingByTypeDto
{
    public string Type { get; set; } = string.Empty;

    public long TransactionCount { get; set; }

    public decimal TotalAmount { get; set; }
}
