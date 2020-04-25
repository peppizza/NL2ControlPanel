using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ConsoleApp
{
    class ConsoleApp
    {

        static void Main(string[] args)
        {
            TcpListener server = null;
            try
            {
                int port = 13000;
                IPAddress localAddr = IPAddress.Parse("172.27.153.76");

                server = new TcpListener(localAddr, port);

                server.Start();

                byte[] bytes = new byte[256];
                string data = null;

                while (true)
                {
                    Console.WriteLine("Waiting for a connection... ");

                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected");

                    data = null;

                    NetworkStream stream = client.GetStream();

                    int i;

                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        data = Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Recieved: {0}", data);

                        switch (data)
                        {
                            case "idle":
                                data = "idle";
                                break;
                            case "test":
                                data = "hello there";
                                break;
                            case "og":
                                data = "opening gates...";
                                break;
                        }
                        byte[] msg = Encoding.ASCII.GetBytes(data);

                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("sent: {0}", data);
                    }

                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                server.Stop();
            }
            
            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }
    }
}