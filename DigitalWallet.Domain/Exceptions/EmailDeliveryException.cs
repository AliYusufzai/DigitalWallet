namespace DigitalWallet.Domain.Exceptions;

public class EmailDeliveryException() : Exception(
    "The confirmation was saved, but email delivery could not be confirmed. " +
    "Retry the original request with the same idempotency key to retrieve its ID, " +
    "then use resend after the cooldown.");
