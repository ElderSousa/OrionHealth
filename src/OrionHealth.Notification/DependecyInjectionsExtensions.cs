using OrionHealth.Notification.Services;
namespace OrionHealth.Notification;
public static class DependecyInjectionsExtensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices();
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IWhatsAppService, WhatsAppService>();

        return services;
    }
}