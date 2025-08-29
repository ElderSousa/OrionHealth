using System.Text;
using System.Text.Json;
using OrionHealth.Notification.Services;
using OrionHealth.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrionHealth.Notification
{
    public class NotificationEvent
    {
        public string? PatientMrn { get; set; }
        public string? Message { get; set; }
    }

    public class NotificationConsumer : BackgroundService
    {
        private readonly ILogger<NotificationConsumer> _logger;
        private readonly RabbitMQConnectionService _rabbitService;
        private readonly IWhatsAppService _whatsAppService;

        private const string ExchangeName = "orionhealth_events_exchange";
        private const string QueueName = "whatsapp_notifications_queue";
        private const string RoutingKey = "resultado.processado.*";

        public NotificationConsumer(
            ILogger<NotificationConsumer> logger,
            RabbitMQConnectionService rabbitService,
            IWhatsAppService whatsAppService)
        {
            _logger = logger;
            _rabbitService = rabbitService;
            _whatsAppService = whatsAppService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var channel = await _rabbitService.GetChannelAsync();

                    channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);
                    channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                    channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);

                    _logger.LogInformation("Exchange, fila e binding configurados.");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var messageJson = Encoding.UTF8.GetString(body);

                        try
                        {
                            var notificationEvent = JsonSerializer.Deserialize<NotificationEvent>(messageJson);
                            if (notificationEvent != null && !string.IsNullOrEmpty(notificationEvent.Message))
                            {
                                var toPhoneNumber = "+5585999321639"; 
                                await _whatsAppService.SendMessageAsync(toPhoneNumber, notificationEvent.Message);
                            }
                            else
                            {
                                _logger.LogWarning("Evento de notificação inválido ou nulo.");
                            }

                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao processar evento de notificação.");
                            channel.BasicNack(ea.DeliveryTag, false, requeue: false);
                        }
                    };

                    channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

                    _logger.LogInformation("Consumidor de notificações iniciado.");

                    // Mantém o consumidor ativo
                    while (channel.IsOpen && !stoppingToken.IsCancellationRequested)
                        await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no consumidor de notificações. Tentando reconectar em 5s...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
    }
}
