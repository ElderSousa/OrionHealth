
using OrionHealth.Application.Interfaces;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using OrionHealth.Infrastructure.HL7;
using RabbitMQ.Client;
namespace OrionHealth.Hl7Processor;

public static class DependecyInjectionsExtensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.RabbitMQSetup(configuration);
        services.AddServices();
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

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
         // Injetando apenas a versão isolada do UseCase
        services.AddScoped<IReceiveOruR01UseCase, Hl7ProcessingUseCase>();

        // Parser HL7
        services.AddSingleton<IHL7Parser, HapiParser>();
        return services;
    }
}