using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace OrionHealth.RabbitMQ;

public class RabbitMQConnectionService : IHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private IConnection _connection = null!;
    private readonly AsyncRetryPolicy<IConnection> _retryPolicy;

    public RabbitMQConnectionService(IConfiguration configuration)
    {
        _configuration = configuration;

        _retryPolicy = Policy<IConnection>
            .Handle<BrokerUnreachableException>()
            .WaitAndRetryAsync(
                5,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, time) => Console.WriteLine($"Falha ao conectar RabbitMQ. Retentando em {time.TotalSeconds}s...")
            );
    }

    public async Task<IModel> GetChannelAsync()
    {
        if (_connection == null || !_connection.IsOpen)
            await ConnectAsync();

        return _connection!.CreateModel();
    }

    private async Task ConnectAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["MessageBroker:HostName"],
            UserName = _configuration["MessageBroker:UserName"],
            Password = _configuration["MessageBroker:Password"],
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = await _retryPolicy.ExecuteAsync(() =>
            Task.FromResult(factory.CreateConnection())
        );

        Console.WriteLine("Conexão RabbitMQ estabelecida!");
    }

    public Task StartAsync(CancellationToken cancellationToken) => ConnectAsync();

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _connection?.Close();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
