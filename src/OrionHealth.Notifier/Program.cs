using OrionHealth.Notifier;
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDependencyInjection(builder.Configuration);

// Registamos o nosso novo consumidor como um serviço em segundo plano
builder.Services.AddHostedService<NotificationConsumer>();

var host = builder.Build();
host.Run();
