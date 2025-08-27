namespace OrionHealth.Notification.Services;

public interface IWhatsAppService
{
    Task SendMessageAsync(string toPhoneNumber, string messageBody);
}
