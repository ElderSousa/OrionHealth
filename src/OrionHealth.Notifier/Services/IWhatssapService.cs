namespace OrionHealth.Notifier.Services
{
    public interface IWhatsAppService
    {
        Task SendMessageAsync(string toPhoneNumber, string messageBody);
    }
}
