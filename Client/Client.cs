using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BasicNetwork.Client;
using BasicNetwork.Packet;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            BasicClient client = new BasicClient("127.0.0.1", 8085);

            client.OnDisconnect += (sender, eventArgs) => Console.WriteLine("Disconnected");
            client.OnTryConnect += (sender, eventArgs) => Console.WriteLine("Try connect...");
            client.OnConnected += (sender, eventArgs) => Console.WriteLine("Connected");

            client.OnReceive += (c, packet) =>
            {
                if (packet.ContainsKey("username") && packet.ContainsKey("message"))
                    Console.WriteLine($"{packet["username"]} > {packet["message"]}");
            };

            Console.Write("Username: ");
            string username = Console.ReadLine();
            Console.Title = "User: " + username;

            client.Connect();

            while (true)
            {
                string msg = Console.ReadLine();
                client.Send(new BasicData { ["username"] = username, ["message"] = msg });
            }

            //client.OnReceive += (c, packet) =>
            //{
            //    Console.WriteLine(c.Handler.RemoteEndPoint);
            //    foreach (var kv in packet)
            //    {
            //        Console.WriteLine($"\t[{kv.Key}] = {kv.Value}");
            //    }
            //};

            //client.Connect();

            //BasicData data = new BasicData();

            //while (true)
            //{
            //    string msg = Console.ReadLine();
            //    if (msg == "send")
            //    {
            //        client.Send(data);
            //        data.Clear();
            //    }
            //    else
            //    {
            //        string[] kv = msg.Split('=');
            //        if (kv.Length != 2)
            //        {
            //            Console.WriteLine("ignored");
            //            continue;
            //        }

            //        data[kv[0].Trim()] = kv[1].Trim();
            //    }
            //}
        }
    }
}
