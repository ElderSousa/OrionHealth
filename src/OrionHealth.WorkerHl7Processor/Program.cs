using OrionHealth.CrossCutting;
using OrionHealth.Hl7Processor;

var builder = Host.CreateApplicationBuilder(args);

// Usamos o nosso método de extensão para registar todas as dependências
builder.Services.AddAppServices(builder.Configuration);

// Registamos o nosso novo consumidor como um serviço em segundo plano
builder.Services.AddHostedService<Hl7MessageConsumer>();

var host = builder.Build();
host.Run();
