using OrionHealth.CrossCutting;
using OrionHealth.Infrastructure.Mllp;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddAppServices(hostContext.Configuration);
        services.AddHostedService<MllpListenerService>();
    })
    .Build();

host.Run();