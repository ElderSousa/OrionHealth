using System.Text;
using System.Text.Json;
using OrionHealth.Notifier.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrionHealth.Notifier
{
    // Criamos uma classe para representar a estrutura do nosso evento de notificação
    public class NotificationEvent
    {
        public string? PatientMrn { get; set; }
        public string? Message { get; set; }
    }

    public class NotificationConsumer : BackgroundService
    {
        private readonly ILogger<NotificationConsumer> _logger;
        private readonly IModel _channel;
        private readonly IWhatsAppService _whatsAppService;

        private const string ExchangeName = "orionhealth_events_exchange";
        private const string QueueName = "whatsapp_notifications_queue";
        private const string RoutingKey = "resultado.processado.*";

        public NotificationConsumer(ILogger<NotificationConsumer> logger, IModel channel, IWhatsAppService whatsAppService)
        {
            _logger = logger;
            _channel = channel;
            _whatsAppService = whatsAppService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            // Garante que o Exchange (Câmbio/Central de Troca) existe
            _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);
            
            // Garante que a nossa fila de notificações existe
            _channel.QueueDeclare(queue: QueueName,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            // Cria a "ligação" (Binding) entre o Exchange e a Fila.
            // Diz ao Exchange: "Qualquer mensagem com esta Routing Key, envie para esta Fila".
            _channel.QueueBind(queue: QueueName,
                               exchange: ExchangeName,
                               routingKey: RoutingKey);

            _logger.LogInformation("Exchange, Fila e Binding para notificações configurados.");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                
                try
                {
                    var notificationEvent = JsonSerializer.Deserialize<NotificationEvent>(messageJson);

                    if (notificationEvent != null && !string.IsNullOrEmpty(notificationEvent.Message))
                    {
                        // --- LÓGICA DE ENVIO DO WHATSAPP ---
                        // ATENÇÃO: Substitua pelo seu número de telefone verificado na Sandbox da Twilio!
                        var toPhoneNumber = "+5585999321639"; 
                        await _whatsAppService.SendMessageAsync(toPhoneNumber, notificationEvent.Message);
                    }
                    else
                    {
                        _logger.LogWarning("Evento de notificação recebido era nulo ou inválido.");
                    }

                    // Confirma ao RabbitMQ que a mensagem foi processada com sucesso.
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar evento de notificação.");
                    // Informa ao RabbitMQ que o processamento falhou e a mensagem não deve ser reenfileirada.
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // Começa a "ouvir" a fila de notificações, usando o consumidor que acabamos de configurar.
            _channel.BasicConsume(queue: QueueName,
                                 autoAck: false, // Confirmação manual é crucial para a resiliência
                                 consumer: consumer);

            _logger.LogInformation("Consumidor de notificações iniciado. Aguardando eventos...");
            return Task.CompletedTask;
        }
    }
}
