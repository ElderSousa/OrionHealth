using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using OrionHealth.Application.Interfaces;
using RabbitMQ.Client;

namespace OrionHealth.Infrastructure.Mllp
{
    public class MllpListenerService : BackgroundService
    {
        private readonly ILogger<MllpListenerService> _logger;
        private readonly IHL7Parser _hl7Parser;
        private readonly IModel _rabbitChannel; // Apenas o canal é necessário aqui
        private readonly X509Certificate2 _serverCertificate;

        private const char START_OF_BLOCK = (char)0x0B;
        private const char END_OF_BLOCK = (char)0x1C;
        private const char CARRIAGE_RETURN = (char)0x0D;
        private const string HL7_PROCESSING_QUEUE = "hl7_processamento";

        // O construtor agora é muito mais simples. Ele apenas "pede" o que precisa.
        public MllpListenerService(ILogger<MllpListenerService> logger, IConfiguration configuration, IHL7Parser hl7Parser, IModel rabbitChannel)
        {
            _logger = logger;
            _hl7Parser = hl7Parser;
            _rabbitChannel = rabbitChannel; // Recebido via injeção de dependência

            var certPath = configuration["Mllp:CertificatePath"];
            var certPassword = "123456"; 
            _serverCertificate = new X509Certificate2(certPath, certPassword);
            
            // A declaração da fila continua a ser uma boa prática para garantir que ela existe.
            _rabbitChannel.QueueDeclare(queue: HL7_PROCESSING_QUEUE,
                                       durable: true,
                                       exclusive: false,
                                       autoDelete: false,
                                       arguments: null);
            _logger.LogInformation("Conexão com RabbitMQ verificada e fila '{QueueName}' declarada.", HL7_PROCESSING_QUEUE);
        }
        
        // O resto do seu código (ExecuteAsync, HandleClientAsync) não precisa de alterações.
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
                    
                    var buffer = new byte[4096];
                    var messageBuilder = new StringBuilder();
                    int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
                    
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    string receivedData = messageBuilder.ToString();
                    int start = receivedData.IndexOf(START_OF_BLOCK);
                    int end = receivedData.IndexOf(END_OF_BLOCK);

                    if (start > -1 && end > start)
                    {
                        string hl7Message = receivedData.Substring(start + 1, end - start - 1).Trim();
                        _logger.LogInformation("Mensagem HL7 recebida. A publicar na fila...");

                        var body = Encoding.UTF8.GetBytes(hl7Message);
                        
                        _rabbitChannel.BasicPublish(exchange: "",
                                                   routingKey: HL7_PROCESSING_QUEUE,
                                                   basicProperties: null,
                                                   body: body);

                        _logger.LogInformation("Mensagem publicada com sucesso na fila '{QueueName}'.", HL7_PROCESSING_QUEUE);
                        
                        string ackMessage = _hl7Parser.CreateAck(hl7Message);
                        var responseMessage = $"{START_OF_BLOCK}{ackMessage}{END_OF_BLOCK}{CARRIAGE_RETURN}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
                        await sslStream.WriteAsync(responseBytes, stoppingToken);
                        _logger.LogInformation("ACK enviado ao cliente.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar cliente.");
                }
                finally
                {
                    _logger.LogInformation("Cliente desconectado.");
                }
            }
        }
    }
}
