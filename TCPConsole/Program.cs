using System;
using System.Net.Sockets;
using TCPClient;

namespace TCPConsole
{
    class Program
    {
        private const string Server = "localhost";
        private const int Port = 15151;

        static void Main(string[] args)
        {
            NL2TelemetryClient client = new NL2TelemetryClient {Server = Server, Port = Port, ClientSocket = new TcpClient(Server, Port)};
            client.SendCommand("idle");
            client.SendCommand("idle");
            client.Close();
        }
    }
}