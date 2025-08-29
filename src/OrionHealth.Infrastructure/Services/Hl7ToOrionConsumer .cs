using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using OrionHealth.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Hl7ToOrionConsumer : BackgroundService
{
    private readonly ILogger<Hl7ToOrionConsumer> _logger;
    private readonly RabbitMQConnectionService _rabbitService;
    private readonly IReceiveOruR01UseCase _receiveOruR01UseCase;
    private const string QueueName = "hl7_to_orion";

    public Hl7ToOrionConsumer(
        ILogger<Hl7ToOrionConsumer> logger,
        RabbitMQConnectionService rabbitService,
        IReceiveOruR01UseCase receiveOruR01UseCase)
    {
        _logger = logger;
        _rabbitService = rabbitService;
        _receiveOruR01UseCase = receiveOruR01UseCase;
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
                    _logger.LogInformation("Mensagem recebida da fila hl7_to_orion: {Hl7Message}", hl7Message);

                    try
                    {
                        var result = await _receiveOruR01UseCase.ExecuteAsync(hl7Message);
                        if (!result.IsSuccess)
                        {
                            _logger.LogWarning("Falha ao salvar no banco: {AckNackMessage}", result.AckNackMessage);
                            channel.BasicNack(ea.DeliveryTag, false, false);
                            return;
                        }

                        channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Mensagem processada e salva com sucesso no banco.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro inesperado ao processar HL7 na fila hl7_to_orion.");
                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                };

                channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
                _logger.LogInformation("Consumidor OrionHealth iniciado. Aguardando mensagens na fila '{QueueName}'.", QueueName);

                while (channel.IsOpen && !stoppingToken.IsCancellationRequested)
                    await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no consumidor OrionHealth. Tentando reconectar em 5 segundos...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
