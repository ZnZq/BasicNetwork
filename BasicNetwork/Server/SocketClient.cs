using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BasicNetwork.Packet;
using Newtonsoft.Json;

namespace BasicNetwork.Server
{
    public class SocketClient
    {
        public delegate void ClientReceiveHandler(SocketClient client, BasicData packet);
        public event EventHandler OnDisconnect;
        public event ClientReceiveHandler OnReceive;
        public Socket Handler { get; protected set; }
        protected bool terminated = false;
        protected NetworkStream networkStream;
        protected StreamWriter writer;
        protected StreamReader reader;
        protected DateTime lastPing = DateTime.Now;
        public bool IsConnected { get; protected set; }

        public SocketClient(Socket handler)
        {
            if (handler == null)
                return;

            IsConnected = true;
            Handler = handler;
            networkStream = new NetworkStream(handler);
            writer = new StreamWriter(networkStream);
            reader = new StreamReader(networkStream);
            Task.Factory.StartNew(execute);
        }

        private void execute()
        {
            while (!terminated) 
            {
                try
                {
                    while (true)
                    {
                        if (!IsConnected)
                            throw new SocketException();

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
                            throw new SocketException();
                        }

                        try
                        {
                            string json = reader.ReadLine();
                            if (json == "ping")
                                continue;
                            OnReceive?.Invoke(this, BasicData.FromJson(json));
                        } catch { }
                        Thread.Sleep(10);
                    }
                }
                catch (SocketException)
                {
                    terminated = true;
                    OnDisconnect?.Invoke(this, EventArgs.Empty);
                } catch { }
            }
        }

        public void Send(BasicData packet)
        {
            try
            {
                string json = JsonConvert.SerializeObject(packet);
                //byte[] bytes = Encoding.UTF8.GetBytes(json);
                //var data = new List<byte>();
                //data.AddRange(BitConverter.GetBytes(bytes.Length));
                //data.AddRange(bytes);
                writer.WriteLine(json);
                writer.Flush();
            } catch (Exception e) { }
        }

        public void Disconnect()
        {
            terminated = true;
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
    }
}