using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BasicNetwork.Server;

namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            BasicServer server = new BasicServer(new IPEndPoint(IPAddress.Any, 8085));

            server.OnConnected += (sender, client) => Console.WriteLine($"{client.Handler.RemoteEndPoint} Connected!");
            server.OnDisconnected += (sender, client) => Console.WriteLine($"{client.Handler.RemoteEndPoint} Disconnected!");
            server.OnReceive += (client, packet) =>
            {
                Console.WriteLine(client.Handler.RemoteEndPoint);
                foreach (var kv in packet)
                {
                    Console.WriteLine($"\t[{kv.Key}] = {kv.Value}");
                }

                foreach (var c in server.Clients)
                {
                    if (c != client)
                        c.Send(packet);
                }
            };

            while (true)
            {
                server.Start();
                Console.WriteLine("Started");

                Console.ReadLine();

                server.Stop();
                Console.WriteLine("Stopped");

                Console.ReadLine();
            }
        }
    }
}
