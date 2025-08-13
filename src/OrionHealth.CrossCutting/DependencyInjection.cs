namespace OrionHealth.CrossCutting;

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
public static class DependencyInjection
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();

        services.AddInfrastructure(configuration);

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

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseOracle(connectionString)
        );

        services.AddScoped<IDbConnection>(sp =>
            new OracleConnection(connectionString)
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IHL7Parser, HapiParser>();

        return services;
    }
}