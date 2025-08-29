namespace OrionHealth.RabbitMQ;

public interface IMessagePublisher
{
    Task PublishAsync(string exchange, string routingKey, byte[] body);
}
