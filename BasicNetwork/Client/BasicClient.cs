using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BasicNetwork.Packet;
using BasicNetwork.Server;

namespace BasicNetwork.Client
{
    public class BasicClient : SocketClient
    {
        public event ClientReceiveHandler OnReceive;
        public event EventHandler OnDisconnect;
        public event EventHandler OnConnected;
        public event EventHandler OnTryConnect;

        private Thread thread;
        public bool AutoReconnect { get; set; } = true;

        public string IP { get; set; }
        public int Port { get; set; }
        public int ConnectTimeout { get; set; } = 2000;

        public BasicClient(string ip, int port) : base(null)
        {
            (IP, Port) = (ip, port);
        }

        public bool Connect()
        {
            try
            {
                Handler = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ExclusiveAddressUse = true,
                    LingerState = new LingerOption(true, 2),
                    NoDelay = true,
                    ReceiveTimeout = 1000,
                    SendTimeout = 1000,
                    Ttl = 42
                };
                OnTryConnect?.Invoke(this, EventArgs.Empty);

                var result = Handler.BeginConnect(IP, Port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(ConnectTimeout, true);
                //Handler.Connect(IP, Port);

                if (IsConnected = Handler.Connected)
                {
                    Handler.EndConnect(result);
                    networkStream = new NetworkStream(Handler);
                    writer = new StreamWriter(networkStream);
                    reader = new StreamReader(networkStream);

                    OnConnected?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                else Disconnect();
            }
            catch
            {
                Disconnect();
            }
            finally
            {
                if (thread == null)
                {
                    thread = new Thread(execute);
                    thread.Start();
                }
            }
            return false;
        }

        private void execute()
        {
            while (!terminated)
            {
                while (true)
                {
                    Thread.Sleep(25);

                    if (!IsConnected && !AutoReconnect)
                        break;

                    if (!IsConnected)
                    {
                        if (!Connect())
                        {
                            Thread.Sleep(2000);
                            continue;
                        }
                    }

                    try
                    {
                        if (DateTime.Now - lastPing > TimeSpan.FromSeconds(3))
                        {
                            lastPing = DateTime.Now;
                            writer.WriteLine("ping");
                            writer.Flush();
                        }
                    }
                    catch
                    {
                        Disconnect();
                        continue;
                    }

                    try
                    {
                        string json = reader.ReadLine();
                        if (json == "ping")
                            continue;
                        OnReceive?.Invoke(this, BasicData.FromJson(json));
                    }
                    catch { }
                }
            }
        }

        public void Disconnect()
        {
            IsConnected = false;
            try
            {
                writer?.Flush();
                writer?.Close();
                writer = null;
                reader?.Close();
                reader = null;
                networkStream?.Flush();
                networkStream?.Close();
                networkStream = null;
                Handler?.Shutdown(SocketShutdown.Both);
            }
            catch { }
            finally
            {
                Handler?.Close();
                Handler = null;
                OnDisconnect?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Stop()
        {
            terminated = true;
            thread?.Abort();
            thread?.Join(100);
            thread = null;
        }
    }
}
