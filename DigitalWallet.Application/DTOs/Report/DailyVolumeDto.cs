namespace DigitalWallet.Application.DTOs.Reports;

public class DailyVolumeDto
{
    public DateTime Date { get; set; }
    public long TransactionCount { get; set; }
    public decimal IncomingAmount { get; set; }
    public decimal OutgoingAmount { get; set; }
}
