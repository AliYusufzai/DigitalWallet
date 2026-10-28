namespace DigitalWallet.Application.Settings;

public class TransferConfirmationSettings
{
    public int ExpirySeconds { get; set; } = 120;
    public int MaxAttempts { get; set; } = 3;
    public int ResendCooldownSeconds { get; set; } = 60;
}
