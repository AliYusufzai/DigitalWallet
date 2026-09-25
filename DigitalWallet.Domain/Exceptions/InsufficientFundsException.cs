namespace DigitalWallet.Domain.Exceptions;

public class InsufficientFundsException : Exception
{
    public decimal CurrentBalance { get; }
    public decimal AttemptedAmount { get; }

    public InsufficientFundsException(decimal currentBalance, decimal attemptedAmount)
        : base($"Insufficient funds. Available: {currentBalance}, Attempted: {attemptedAmount}")
    {
        CurrentBalance = currentBalance;
        AttemptedAmount = attemptedAmount;
    }
}
