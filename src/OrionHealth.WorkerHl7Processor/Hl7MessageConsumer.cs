using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrionHealth.Hl7Processor
{
    public class Hl7MessageConsumer : BackgroundService
    {
        private readonly ILogger<Hl7MessageConsumer> _logger;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        private const string QueueName = "hl7_processamento";

        public Hl7MessageConsumer(ILogger<Hl7MessageConsumer> logger, IModel channel, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _channel = channel;
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var hl7Message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Mensagem recebida da fila: {Hl7Message}", hl7Message);

                try
                {
                    // Criamos um 'scope' para obter as nossas dependências
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var useCase = scope.ServiceProvider.GetRequiredService<IReceiveOruR01UseCase>();
                        await useCase.ExecuteAsync(hl7Message);
                    }

                    // Confirmamos ao RabbitMQ que a mensagem foi processada com sucesso
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Mensagem processada e ACK enviado ao RabbitMQ.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar a mensagem HL7. A mensagem será rejeitada.");
                    // Rejeitamos a mensagem, o que a pode enviar para uma "dead-letter queue" se configurada
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            // Começamos a "ouvir" a fila
            _channel.BasicConsume(queue: QueueName,
                                 autoAck: false, // Importante: confirmamos manualmente
                                 consumer: consumer);

            _logger.LogInformation("Consumidor iniciado. A aguardar mensagens na fila '{QueueName}'.", QueueName);
            return Task.CompletedTask;
        }
    }
}
