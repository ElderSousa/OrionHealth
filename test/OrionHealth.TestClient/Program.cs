using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {

        //Console.WriteLine("Aguardando o servidor iniciar... (5 segundo)");
        //Thread.Sleep(5000);

        //Console.WriteLine("Aguardando o servidor iniciar... (5 segundo)");
        //Thread.Sleep(5000);

        string serverAddress = "localhost";
        int port = 1080;

        string hl7Data = """
        MSH|^~\&|HOSPITAL_HIS|MAIN_HOSPITAL|LAB_SYSTEM|CENTRAL_LAB|20250820140000||ADT^A08|MSGID67890|P|2.5.1
        EVN|A08|20250820140000
        PID|1||12345^^^MRN||Silva de Oliveira^Joao||19800515|M
        """;

        string mllpMessage = (char)0x0B + hl7Data.Replace("\r\n", "\r") + (char)0x1C + (char)0x0D;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Tentando conectar em {serverAddress}:{port}...");

        try
        {
            using var client = new TcpClient(serverAddress, port);

            await using var sslStream = new SslStream(
                client.GetStream(),
                false,
                (sender, certificate, chain, sslPolicyErrors) =>
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Aviso: Aceitando certificado autoassinado do servidor.");
                    return true;
                });

            await sslStream.AuthenticateAsClientAsync(serverAddress);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Conexão TLS estabelecida com sucesso!");

            byte[] buffer = Encoding.UTF8.GetBytes(mllpMessage);
            await sslStream.WriteAsync(buffer);
            await sslStream.FlushAsync();

            Console.ResetColor();
            Console.WriteLine("\n>>> MENSAGEM ENVIADA:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(hl7Data);

            byte[] responseBuffer = new byte[1024];
            int bytesRead = await sslStream.ReadAsync(responseBuffer);
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

            Console.ResetColor();
            Console.WriteLine("\n<<< RESPOSTA RECEBIDA:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nOcorreu um erro: {ex.Message}");
        }
        finally
        {
            Console.ResetColor();
            Console.WriteLine("\nConexão fechada.");
        }
    }
}