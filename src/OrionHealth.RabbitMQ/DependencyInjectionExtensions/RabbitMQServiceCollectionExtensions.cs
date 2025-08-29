using Microsoft.Extensions.DependencyInjection;

namespace OrionHealth.RabbitMQ
{
    public static class RabbitMQServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMQMessaging(this IServiceCollection services)
        {
            services.AddSingleton<RabbitMQConnectionService>();
            services.AddHostedService(sp => sp.GetRequiredService<RabbitMQConnectionService>());
            services.AddSingleton<IMessagePublisher, MessagePublisherService>();
            return services;
        }
    }
}
