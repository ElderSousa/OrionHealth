
using OrionHealth.Application.Interfaces;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using OrionHealth.Infrastructure.HL7;
using OrionHealth.RabbitMQ;
namespace OrionHealth.Hl7Processor;

public static class DependecyInjectionsExtensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices();
        return services;
    }
    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IReceiveOruR01UseCase, Hl7ProcessingUseCase>();
        services.AddSingleton<IHL7Parser, HapiParser>();
        services.AddSingleton<IHostedService, RabbitMQConnectionService>();
        services.AddSingleton<RabbitMQConnectionService>();
        return services;
    }
}