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
using RabbitMQ.Client; // Adicione este using

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

            // --- INÍCIO DA CORREÇÃO DEFINITIVA ---
            // Registramos a conexão e o canal do RabbitMQ como Singletons.
            // A aplicação irá criar e gerir uma única conexão para ser reutilizada.
            services.AddSingleton<IConnection>(sp =>
            {
                var factory = new ConnectionFactory() { HostName = configuration["MessageBroker:HostName"] };
                return factory.CreateConnection();
            });

            services.AddSingleton<IModel>(sp =>
            {
                var connection = sp.GetRequiredService<IConnection>();
                return connection.CreateModel();
            });
            // --- FIM DA CORREÇÃO DEFINITIVA ---

            return services;
        }
    }
}
