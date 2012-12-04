using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Configuration;
using System.Collections;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Portee
{
    public class SocketClient
    {
        public delegate void ReceivedServerDataHandlerDel(byte[] Data, TcpClient Client);
        public List<TcpClient> ActiveClients = new List<TcpClient>();
        private TcpClient tcpClient;
        private Thread connectThread;
        private bool Active = true;
        private int Port;
        private string Server;
        public ReceivedServerDataHandlerDel ReceivedDataHandler = null;

        public SocketClient(string Server, int Port)
        {
            this.Server = Server;
            this.Port = Port;
            Start();
        }

        public void Start()
        {
            this.connectThread = new Thread(new ThreadStart(ConnectToServer));
            this.connectThread.Start();
        }

        public void Shutdown()
        {
            this.Active = false;
            Console.WriteLine("Disconnecting active clients");
            this.connectThread.Abort();
            Console.WriteLine("Stopping socket client service");
            this.tcpClient.Close();
        }

        private void ConnectToServer()
        {
            while (true)
            try
            {
                this.tcpClient = new TcpClient();
                Console.WriteLine("Attempting to connect to: {0}:{1}", Server, Port);
                this.tcpClient.Connect(Server, Port);
                Console.WriteLine("Connected to: {0}", this.tcpClient.Client.RemoteEndPoint);
                //Create a thread to handle the client
                Thread clientThread = new Thread(ClientHandler);
                clientThread.Start(this.tcpClient);
                break;
            }
            catch
            {
                Console.WriteLine("Connection failed, retrying...");
                Thread.Sleep(1000);
            }
        }

        private void ClientHandler(Object client)
        {
            TcpClient User = (TcpClient)client;
            ActiveClients.Add(User);

            //Main Receiver
            while (Active)
            {
                byte[] dataSegment = new byte[1048576];
                int bytesRead = 0;
                byte[] ReceivedData;
                try
                {
                    //Blocks until data is received
                    bytesRead = User.GetStream().Read(dataSegment, 0, 1048576);
                    ReceivedData = new byte[bytesRead];
                    Buffer.BlockCopy(dataSegment, 0, ReceivedData, 0, bytesRead);
                }

                catch //Socket Error
                {
                    Console.WriteLine("Client Disconnected (Socket Read Error): {0}", User.Client.RemoteEndPoint);
                    break;
                }
                if (!Active)
                    break;

                if (bytesRead == 0) //Disconnect
                {
                    Console.WriteLine("Client Disconnected: {0}", User.Client.RemoteEndPoint);
                    break;
                }

                if (ReceivedDataHandler != null)
                    ReceivedDataHandler(ReceivedData, User);
                else
                    Console.WriteLine("Data received, but no method has registered to handle it");
            }
            //Close client TCP stream
            ActiveClients.Remove(User);
            User.Close();
            ConnectToServer();
        }

        //Send a message to all active NetworkStreams
        public void Broadcast(byte[] data)
        {
            Parallel.ForEach(ActiveClients, User =>
                                                {
                                                    try
                                                    {
                                                        User.GetStream().Write(data, 0, data.Length);
                                                    }
                                                    catch { }
                                                });
        }

        private void Disconnect(NetworkStream clientStream)
        {
            clientStream.Close();
        }

        ~SocketClient()
        {
            this.Shutdown();
        }
    }
}