namespace OrionHealth.Infrastructure.Mllp;

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;
using System.Text;
using OrionHealth.Application.UseCases.ReceiveOruR01.Interfaces;
using System.Configuration;
using Microsoft.Extensions.Configuration;

public class MllpListenerService : BackgroundService
{
    private readonly ILogger<MllpListenerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly X509Certificate2 _serverCertificate;

    private const char START_OF_BLOCK = (char)0x0B;
    private const char END_OF_BLOCK = (char)0x1C;
    private const char CARRIAGE_RETURN = (char)0x0D;

    public MllpListenerService(ILogger<MllpListenerService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        var certPath = configuration["Mllp:CertificatePath"];
        var certPassword = "123456";
        _serverCertificate = new X509Certificate2(certPath!, certPassword);
        _logger.LogInformation("Certificado do servidor '{Subject}' carregado com sucesso.", _serverCertificate.Subject);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, 1080);
        listener.Start();
        _logger.LogInformation("Servidor MLLP/TLS iniciado na porta 1080. Aguardando conexões...");

        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
            _logger.LogInformation("Cliente conectado de {RemoteEndPoint}", client.Client.RemoteEndPoint);
            _ = HandleClientAsync(client, stoppingToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        using (client)
        await using (var sslStream = new SslStream(client.GetStream(), false))
        {
            try
            {
                await sslStream.AuthenticateAsServerAsync(
                    serverCertificate: _serverCertificate,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: true);
                
                _logger.LogInformation("Handshake TLS bem-sucedido. Conexão segura estabelecida.");

                var buffer = new byte[4096];
                var messageBuilder = new StringBuilder();
                int bytesRead;

                while ((bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, stoppingToken)) > 0)
                {
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    string receivedData = messageBuilder.ToString();
                    int start = receivedData.IndexOf(START_OF_BLOCK);
                    int end = receivedData.IndexOf(END_OF_BLOCK);

                    if (start > -1 && end > start)
                    {
                        string hl7Message = receivedData.Substring(start + 1, end - start - 1);
                        _logger.LogInformation("Mensagem HL7 segura recebida.");

                        await using (var scope = _serviceProvider.CreateAsyncScope())
                        {
                            var useCase = scope.ServiceProvider.GetRequiredService<IReceiveOruR01UseCase>();
                            var result = await useCase.ExecuteAsync(hl7Message);
                            
                            var responseMessage = $"{START_OF_BLOCK}{result.AckNackMessage}{END_OF_BLOCK}{CARRIAGE_RETURN}";
                            byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                            await sslStream.WriteAsync(responseBytes, stoppingToken);
                            await sslStream.FlushAsync(stoppingToken); // Força o envio.
                            _logger.LogInformation("Resposta {Status} segura enviada.", result.IsSuccess ? "ACK" : "NAK");
                        }
                        
                        await sslStream.ShutdownAsync();

                        break; 
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar cliente seguro.");
            }
            finally
            {
                _logger.LogInformation("Cliente seguro desconectado.");
            }
        }
    }
}