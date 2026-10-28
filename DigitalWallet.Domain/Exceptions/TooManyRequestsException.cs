namespace DigitalWallet.Domain.Exceptions;

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(int retryAfterSeconds)
        : base("Please wait before requesting another confirmation code.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int RetryAfterSeconds { get; }
}
