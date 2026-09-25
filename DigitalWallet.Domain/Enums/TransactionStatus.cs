namespace DigitalWallet.Domain.Enums;

public enum TransactionStatus
{
    Pending,    // initiated but not completed
    Completed,  // successfully done
    Failed,     // something went wrong
    Reversed    // was completed but then reversed
}