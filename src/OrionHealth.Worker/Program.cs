using OrionHealth.CrossCutting;
using OrionHealth.Infrastructure.Mllp;

var builder = Host.CreateApplicationBuilder(args);

// --- INÍCIO DA CORREÇÃO DEFINITIVA ---
// Limpa as fontes de configuração padrão para ter controle total.
builder.Configuration.Sources.Clear();

// Configura o host para ler os arquivos appsettings a partir do diretório base da aplicação.
// Isso garante que ele encontre os arquivos, não importa como o processo é iniciado.
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
// --- FIM DA CORREÇÃO DEFINITIVA ---

builder.Services.AddAppServices(builder.Configuration);
builder.Services.AddHostedService<MllpListenerService>();

var host = builder.Build();
host.Run();
