using RabbitMQ.Client;

namespace OrionHealth.RabbitMQ;

public class MessagePublisherService : IMessagePublisher
{
    private readonly RabbitMQConnectionService _rabbitService;

    public MessagePublisherService(RabbitMQConnectionService rabbitService)
    {
        _rabbitService = rabbitService;
    }

    public async Task PublishAsync(string exchange, string routingKey, byte[] body)
    {
        using var channel = await _rabbitService.GetChannelAsync();
        channel.BasicPublish(exchange, routingKey, null, body);
        Console.WriteLine("Mensagem enviada!");
    }
}
