using System.Net.Sockets;
using TCPClient;

namespace TCPConsole
{
    class Program
    {
        private const int Port = 15151;
        public static string Server = string.Empty;

        static void Main(string[] args)
        {
            //Server = args.Length == 0 ? "localhost" : args[0];
            //
            //var client = new NL2TelemetryClient {Server = Server, Port = Port, ClientSocket = new TcpClient(Server, Port)};
            //client.SendCommand("idle");
            //client.SendCommand("idle");
            //client.Close();
            GpioController controller = new GpioController{Pin = 4};
            controller.LedBlink();
        }
    }
}