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
    internal sealed class SocketClient : SocketBase
    {
        public delegate void ReceivedServerDataHandlerDel(byte[] data, TcpClient client);
        private TcpClient _tcpClient;
        private Thread _connectThread;
        private bool _active = true;
        private readonly int _port;
        private readonly string _server;

        public SocketClient(string server, int port)
        {
            this._server = server;
            this._port = port;
            Start();
        }

        public override void Start()
        {
            this._connectThread = new Thread(new ThreadStart(ConnectToServer));
            this._connectThread.Start();
        }

        public override void Shutdown()
        {
            this._active = false;
            Console.WriteLine("Disconnecting active clients");
            this._connectThread.Abort();
            Console.WriteLine("Stopping socket client service");
            this._tcpClient.Close();
        }

        private void ConnectToServer()
        {
            while (true)
            try
            {
                this._tcpClient = new TcpClient();
                Console.WriteLine("Attempting to connect to: {0}:{1}", _server, _port);
                this._tcpClient.Connect(_server, _port);
                Console.WriteLine("Connected to: {0}", this._tcpClient.Client.RemoteEndPoint);
                //Create a thread to handle the client
                Thread clientThread = new Thread(ClientHandler);
                clientThread.Start(this._tcpClient);
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
            while (_active)
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
                if (!_active)
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

        ~SocketClient()
        {
            this.Shutdown();
        }
    }
}