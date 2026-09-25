namespace DigitalWallet.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with identifier '{key}' was not found.") { }
}
