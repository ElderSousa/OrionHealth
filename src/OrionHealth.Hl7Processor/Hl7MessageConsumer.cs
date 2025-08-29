using System.Text;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using OrionHealth.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Hl7MessageConsumer : BackgroundService
{
    private readonly ILogger<Hl7MessageConsumer> _logger;
    private readonly RabbitMQConnectionService _rabbitService;
    private readonly IReceiveOruR01UseCase _hl7ProcessingUseCase;
    private readonly IMessagePublisher _messagePublisher;

    private const string QueueName = "hl7_processamento";
    private const string OrionQueueName = "hl7_to_orion";

    public Hl7MessageConsumer(
        ILogger<Hl7MessageConsumer> logger,
        RabbitMQConnectionService rabbitService,
        IReceiveOruR01UseCase hl7ProcessingUseCase,
        IMessagePublisher messagePublisher)
    {
        _logger = logger;
        _rabbitService = rabbitService;
        _hl7ProcessingUseCase = hl7ProcessingUseCase;
        _messagePublisher = messagePublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var channel = await _rabbitService.GetChannelAsync();

                channel.QueueDeclare(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var hl7Message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("Mensagem recebida: {Hl7Message}", hl7Message);

                    try
                    {
                        var result = await _hl7ProcessingUseCase.ExecuteAsync(hl7Message);

                        if (!result.IsSuccess)
                        {
                            _logger.LogWarning("Falha no processamento: {AckNackMessage}", result.AckNackMessage);
                            channel.BasicNack(ea.DeliveryTag, false, false);
                            return;
                        }

                        channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Mensagem processada com sucesso.");

                        // Publica para o microserviço OrionHealth salvar no banco
                        var orionBody = Encoding.UTF8.GetBytes(hl7Message);
                        await _messagePublisher.PublishAsync(
                            exchange: "",
                            routingKey: OrionQueueName,
                            body: orionBody
                        );
                        _logger.LogInformation("Mensagem publicada na fila '{QueueName}' para OrionHealth.", OrionQueueName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar HL7. Mensagem rejeitada.");
                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                };

                channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
                _logger.LogInformation("Consumidor iniciado. Aguardando mensagens na fila '{QueueName}'.", QueueName);

                while (channel.IsOpen && !stoppingToken.IsCancellationRequested)
                    await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no consumidor. Tentando reconectar em 5 segundos...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
