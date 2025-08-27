using OrionHealth.Notification.Services;
using RabbitMQ.Client;
namespace OrionHealth.Notification;
public static class DependecyInjectionsExtensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices();
        services.RabbitMQSetup(configuration);

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IWhatsAppService, WhatsAppService>();

        return services;
    }

    private static IServiceCollection RabbitMQSetup(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration["MessageBroker:HostName"],
                AutomaticRecoveryEnabled = true,   // reconecta automático
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10) // tenta de novo a cada 10s
            };
            return factory.CreateConnection();
        });

        services.AddSingleton(sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            return connection.CreateModel();
        });

        return services;
    }
}