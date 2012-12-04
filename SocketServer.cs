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
    internal class SocketServer
    {
        public delegate void ReceivedServerDataHandlerDel(byte[] Data, TcpClient Client);
        public List<TcpClient> ActiveClients = new List<TcpClient>();
        public ASCIIEncoding encoder = new ASCIIEncoding();
        private TcpListener tcpListener;
        private Thread listenThread;
        private bool Active = true;
        public IPAddress Host;
        public int Port;
        public ReceivedServerDataHandlerDel ReceivedDataHandler = null;

        public SocketServer(IPAddress Host, int Port)
        {
            this.Host = Host;
            this.Port = Port;
            Start();
        }

        public void Start()
        {
            this.tcpListener = new TcpListener(Host,Port);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        public void Shutdown()
        {
            this.Active = false;
            Console.WriteLine("Disconnecting active clients");
            this.listenThread.Abort();
            Console.WriteLine("Shutting down server");
            this.tcpListener.Stop();
        }

        private void ListenForClients()
        {
            while (true)
                try
                {
                    this.tcpListener.Start();
                    break;
                }
                catch
                {
                    Console.WriteLine("Failed to bind local socket on {0}:{1}, retrying...", Host, Port);
                    Thread.Sleep(1000);
                    continue;
                }

            Console.WriteLine("Listening for connections on {0}:{1}",Host,Port);

            while (true)
            {
                //Blocks until a client connects
                TcpClient client = tcpListener.AcceptTcpClient();
                Console.WriteLine("Client Connected: {0}",client.Client.RemoteEndPoint);
                //Create a thread to handle the client
                Thread clientThread = new Thread(ClientHandler);
                clientThread.Start(client);
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

                try
                {
                    //Blocks until data is received
                    bytesRead = User.GetStream().Read(dataSegment, 0, 1048576);
                    if (bytesRead == 0) //Disconnected
                    {
                        Console.WriteLine("Client Disconnected: {0}", User.Client.RemoteEndPoint);
                        break;
                    }
                    //Resize the dataSegment to the actual packet length
                    Array.Resize(ref dataSegment, bytesRead);
                    //Dispatch the incoming packet
                    if (ReceivedDataHandler != null)
                        ReceivedDataHandler(dataSegment, User);
                    else
                        Console.WriteLine("Data received, but no method has registered to handle it");
                }
                //Socket Error
                catch
                {
                    Console.WriteLine("Client Disconnected (Socket Read Error): {0}", User.Client.RemoteEndPoint);
                    break;
                }
                if (!Active)
                    break;
            }
            //Close client TCP stream
            ActiveClients.Remove(User);
            User.Close();
        }

        //Send a message to a client
        private static void Send(TcpClient User, string message)
        {
            throw new NotImplementedException();
        }

        //Send a message to all active NetworkStreams
        public void Broadcast(string message)
        {
            throw new NotImplementedException();
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

        //Send a message to multiple clients
        private void Multicast(ArrayList TargetClients, byte[] data)
        {
            throw new NotImplementedException();
        }

        private void Disconnect(NetworkStream clientStream)
        {
            clientStream.Close();
        }

        ~SocketServer()
        {
            this.Shutdown();
        }
    }
}