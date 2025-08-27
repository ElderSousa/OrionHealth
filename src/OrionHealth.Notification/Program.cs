
using OrionHealth.Notification;

var builder = Host.CreateApplicationBuilder(args);

// --- INÍCIO DA CORREÇÃO DEFINITIVA ---
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
// --- FIM DA CORREÇÃO DEFINITIVA ---

builder.Services.AddDependencyInjection(builder.Configuration);
builder.Services.AddHostedService<NotificationConsumer>();

var host = builder.Build();
host.Run();
