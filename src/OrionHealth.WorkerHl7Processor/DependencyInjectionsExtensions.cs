
public static class DependecyInjectionsExtensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.RabbitMQSetup(configuration);

        return services;
    }

    private static IServiceCollection RabbitMQSetup(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var factory = new RabbitMQ.Client.ConnectionFactory()
            {
                HostName = configuration["MessageBroker:HostName"]
            };
            return factory.CreateConnection();
        });

        services.AddSingleton(sp =>
        {
            var connection = sp.GetRequiredService<RabbitMQ.Client.IConnection>();
            return connection.CreateModel();
        });

        return services;
    }
}