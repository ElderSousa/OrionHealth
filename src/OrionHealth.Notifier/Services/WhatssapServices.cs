using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace OrionHealth.Notifier.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;

        public WhatsAppService(ILogger<WhatsAppService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _accountSid = configuration["Twilio:AccountSid"]!;
            _authToken = configuration["Twilio:AuthToken"]!;
            _fromPhoneNumber = configuration["Twilio:FromPhoneNumber"]!;

            // Inicializa o cliente da Twilio
            TwilioClient.Init(_accountSid, _authToken);
        }

        public async Task SendMessageAsync(string toPhoneNumber, string messageBody)
        {
            try
            {
                _logger.LogInformation("A enviar mensagem de WhatsApp para {ToPhoneNumber}", toPhoneNumber);

                var messageOptions = new CreateMessageOptions(
                    new PhoneNumber($"whatsapp:{toPhoneNumber}"))
                {
                    From = new PhoneNumber(_fromPhoneNumber),
                    Body = messageBody
                };

                var message = await MessageResource.CreateAsync(messageOptions);

                _logger.LogInformation("Mensagem enviada com sucesso. SID: {MessageSid}", message.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar a mensagem de WhatsApp.");
            }
        }
    }
}
