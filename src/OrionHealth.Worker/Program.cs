using OrionHealth.CrossCutting;
using OrionHealth.Infrastructure.Mllp;
using Microsoft.Extensions.Hosting;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddAppServices(hostContext.Configuration);

        services.AddHostedService<MllpListenerService>();
    })
    .Build();

host.Run();