namespace DigitalWallet.Application.Interfaces.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
