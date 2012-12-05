using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Portee
{
    internal abstract class SocketBase
    {
        public List<TcpClient> ActiveClients = new List<TcpClient>();
        public SocketServer.ReceivedServerDataHandlerDel ReceivedDataHandler = null;

        public virtual void Send(TcpClient user, string message)
        {
            var packet = Encoding.ASCII.GetBytes(message);
            user.GetStream().WriteAsync(packet, 0, packet.Length);
        }

        public virtual void Broadcast(string message)
        {
            var packet = Encoding.ASCII.GetBytes(message);
            Parallel.ForEach(ActiveClients, user =>
                                                {
                                                    try
                                                    {
                                                        user.GetStream().WriteAsync(packet, 0, packet.Length);
                                                    }
                                                    catch
                                                    {
                                                        Disconnect(user);
                                                    }
                                                });
        }

        public virtual void Broadcast(byte[] data)
        {
            Parallel.ForEach(ActiveClients, user =>
                                                {
                                                    try
                                                    {
                                                        user.GetStream().WriteAsync(data, 0, data.Length);
                                                    }
                                                    catch
                                                    {
                                                        Disconnect(user);
                                                    }
                                                });
        }

        public virtual void Multicast(List<TcpClient> targetClients, byte[] data)
        {
            Parallel.ForEach(targetClients, user =>
            {
                try
                {
                    user.GetStream().WriteAsync(data, 0, data.Length);
                }
                catch
                {
                    Disconnect(user);
                }
            });
        }

        public virtual void Disconnect(TcpClient user)
        {
            try
            {
                user.GetStream().Close();
            }
            catch { }
            finally
            {
                ActiveClients.Remove(user);
            }
        }

        public abstract void Shutdown();
        public abstract void Start();
    }
}