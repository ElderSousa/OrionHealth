using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OrionHealth.Application.Interfaces.Persistence;
using OrionHealth.Application.Interfaces;
using OrionHealth.Infrastructure.Persistence;
using OrionHealth.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using OrionHealth.Application.UseCases.ReceiveOruR01;
using OrionHealth.Infrastructure.HL7;
using RabbitMQ.Client;

namespace OrionHealth.CrossCutting
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.AddLogging();
            
            return services;
        }

        private static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IReceiveOruR01UseCase, ReceiveOruR01UseCase>();
            return services;
        }

        private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("OracleConnection");
            services.AddDbContext<ApplicationDbContext>(options => options.UseOracle(connectionString));
            services.AddScoped<IDbConnection>(sp => new OracleConnection(connectionString));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IHL7Parser, HapiParser>();
            services.RabbitMQSetup(configuration);
            
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
}
