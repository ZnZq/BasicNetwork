using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BasicNetwork.Packet;

namespace BasicNetwork.Server
{
    public class BasicServer
    {
        public delegate void ServerHandler(BasicServer sender, SocketClient client);

        public event ServerHandler OnConnected;
        public event ServerHandler OnDisconnected;
        public event SocketClient.ClientReceiveHandler OnReceive;

        public Socket Socket { get; private set; }

        private List<SocketClient> clients = new List<SocketClient>();
        public IReadOnlyList<SocketClient> Clients { get => clients; }
        public bool IsStart { get; private set; }

        public IPEndPoint IPEndPoint;

        public BasicServer(IPEndPoint endPoint) => IPEndPoint = endPoint;

        public bool Start()
        {
            try
            {
                Stop();
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ExclusiveAddressUse = true,
                    LingerState = new LingerOption(true, 2),
                    NoDelay = true,
                    ReceiveTimeout = 1000,
                    SendTimeout = 1000,
                    Ttl = 42
                };

                Socket.Bind(IPEndPoint);
                Socket.Listen(0xFF);
                Socket.BeginAccept(AcceptCallback, Socket);
                return IsStart = true;
            }
            catch
            {
                return IsStart = false;
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket) ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
                SocketClient client = new SocketClient(handler);
                client.OnDisconnect += ClientOnOnDisconnect;
                client.OnReceive += OnReceive;
                clients.Add(client);
                OnConnected?.Invoke(this, client);
                Socket.BeginAccept(AcceptCallback, Socket);
            }
            catch { }
        }

        private void ClientOnOnDisconnect(object sender, EventArgs e)
        {
            if (IsStart)
                clients.Remove((SocketClient) sender);

            OnDisconnected?.Invoke(this, (SocketClient)sender);
        }

        public void Stop()
        {
            IsStart = false;

            while (clients.Count > 0)
            {
                SocketClient client = clients[0];
                clients.RemoveAt(0);
                client.Disconnect();
            }

            Socket?.Close();
            Socket = null;
        }
    }
}
