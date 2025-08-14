using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {

        string serverAddress = "localhost";
        int port = 1080;

        string hl7Data = """
        MSH|^~\&|TEST_CLIENT|TEST_HOSPITAL|OrionHealth|MainHospital|20250803223000||ORU^R01|MSG_FINAL_TEST|P|2.5.1|||UTF-8
        PID|1||98765^^^MRN||Silva^Maria||19750412|F
        OBR|1|LABORDER_ID^LAB||SODIO^SODIO|||20250803223000|||||||||||||F
        OBX|1|NM|NA^Sodio||142|mEq/L|||||F
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
                (sender, certificate, chain, sslPolicyErrors) => {
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