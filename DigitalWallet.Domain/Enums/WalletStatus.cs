namespace DigitalWallet.Domain.Enums;

public enum WalletStatus
{
    Active,     // wallet is usable
    Suspended,  // temporarily blocked
    Closed      // permanently closed
}