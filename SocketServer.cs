using System;
using System.Collections.Generic;
using System.Linq;
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
    internal sealed class SocketServer : SocketBase
    {
        public delegate void ReceivedServerDataHandlerDel(byte[] data, TcpClient client);
        private TcpListener _tcpListener;
        private Thread _listenThread;
        private bool _active = true;
        private readonly IPAddress _host;
        private readonly int _port;

        public SocketServer(IPAddress host, int port)
        {
            this._host = host;
            this._port = port;
            Start();
        }

        public override void Start()
        {
            this._tcpListener = new TcpListener(_host,_port);
            this._listenThread = new Thread(new ThreadStart(ListenForClients));
            this._listenThread.Start();
        }

        public override void Shutdown()
        {
            this._active = false;
            Console.WriteLine("Disconnecting active clients");
            this._listenThread.Abort();
            Console.WriteLine("Shutting down server");
            this._tcpListener.Stop();
        }

        private void ListenForClients()
        {
            while (_active)
                try
                {
                    this._tcpListener.Start();
                    break;
                }
                catch
                {
                    Console.WriteLine("Failed to bind local socket on {0}:{1}, retrying...", _host, _port);
                    Thread.Sleep(1000);
                    continue;
                }

            Console.WriteLine("Listening for connections on {0}:{1}",_host,_port);

            while (_active)
            {
                //Blocks until a client connects
                var client = _tcpListener.AcceptTcpClient();
                Console.WriteLine("Client Connected: {0}",client.Client.RemoteEndPoint);
                //Create a thread to handle the client
                var clientThread = new Thread(ClientHandler);
                clientThread.Start(client);
            }
        }

        private void ClientHandler(Object client)
        {
            var user = (TcpClient)client;
            ActiveClients.Add(user);

            //Main Receiver
            while (_active)
            {
                var dataSegment = new byte[1048576];

                try
                {
                    //Blocks until data is received
                    var bytesRead = user.GetStream().Read(dataSegment, 0, 1048576);
                    if (bytesRead == 0) //Disconnected
                    {
                        Console.WriteLine("Client Disconnected: {0}", user.Client.RemoteEndPoint);
                        break;
                    }
                    //Resize the dataSegment to the actual packet length
                    Array.Resize(ref dataSegment, bytesRead);
                    //Dispatch the incoming packet
                    if (ReceivedDataHandler != null)
                        ReceivedDataHandler(dataSegment, user);
                    else
                        Console.WriteLine("Data received, but no method has registered to handle it");
                }
                //Socket Error
                catch
                {
                    Console.WriteLine("Client Disconnected (Socket Read Error): {0}", user.Client.RemoteEndPoint);
                    break;
                }
                if (!_active)
                    break;
            }
            //Close client TCP stream
            ActiveClients.Remove(user);
            user.Close();
        }

        ~SocketServer()
        {
            this.Shutdown();
        }
    }
}