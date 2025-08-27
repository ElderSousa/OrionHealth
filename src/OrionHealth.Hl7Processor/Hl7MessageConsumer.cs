using System.Text;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrionHealth.Hl7Processor;

public class Hl7MessageConsumer : BackgroundService
{
    private readonly ILogger<Hl7MessageConsumer> _logger;
    private readonly IModel _channel;
    private readonly IReceiveOruR01UseCase _receiveOruR01UseCase;
    private const string QueueName = "hl7_processamento";


    public Hl7MessageConsumer(ILogger<Hl7MessageConsumer> logger, IModel channel, IReceiveOruR01UseCase receiveOruR01UseCase)
    {
        _logger = logger;
        _channel = channel;
        _receiveOruR01UseCase = receiveOruR01UseCase;

         // Garantir que a fila exista
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
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
                var result = await _receiveOruR01UseCase.ExecuteAsync(hl7Message);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Processamento falhou. NACK enviado. Motivo: {AckNackMessage}", result.AckNackMessage);
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Mensagem processada com sucesso e ACK enviado.");
               
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
        // Mantém o background service vivo enquanto não for cancelado
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
