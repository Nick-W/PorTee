using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;

namespace Portee
{
    class Program
    {
        //Portee, a Cross-platform light-weight traffic replicator and aggregator.  A more flexible solution than mkfifo & nc | tee.
        //Supports bidirectional or one-way traffic
        //Uses: Buggy services that don't play well with multiple connections
        //      Consume remote high-traffic services only once and distribute it to multiple clients
        //      Aggregate traffic to a host (Be mindful of split packets/streams! We're violating TCP/IP here)

        public static string Remotehost;
        public static int Remoteport;
        public static IPAddress Localhost = new IPAddress(0);
        public static int Localport;
        public static optionflags Options;
        public static SocketServer Server;
        public static SocketClient Client;

        static void Main(string[] args)
        {
            Console.WriteLine("Portee v.{0} by Nick Wilson",
                  Assembly.GetExecutingAssembly().GetName().Version);
            try
            {
                var unknownArgs = p.Parse(args);
                if (unknownArgs.Count > 0 || (Options & optionflags.SHOWHELP) == optionflags.SHOWHELP)
                {
                    var err = new StringBuilder();
                    foreach (var a in unknownArgs)
                        err.AppendLine(String.Format("Invalid argument: {0}", a));
                    ShowHelp(err.ToString().Trim());
                    Environment.Exit(1);
                }
            }
            catch (OptionException e)
            {
                ShowHelp("Error: " + e.Message);
                Environment.Exit(1);
            }

            if (Options.HasFlag(optionflags.PROMPT))
            {
                Console.Write("Remote Host: ");
                Remotehost = Console.ReadLine();
                Console.Write("Remote Port: ");
                Remoteport = Localport = int.Parse(Console.ReadLine());
            }

            //Sanity check options
            if (Options.HasFlag(optionflags.READONLY) && Options.HasFlag(optionflags.WRITEONLY))
            {
                ShowHelp("Error: dividing by 0 are we?");
            }
            if (String.IsNullOrEmpty(Remotehost) && (Remoteport < 1 || Remoteport > 65535))
            {
                ShowHelp("Error: Remote host/port are missing or invalid");
                Environment.Exit(1);
            }
            if (Localport < 1 || Localport > 65535)
            {
                ShowHelp("Error: Localport is invalid");
                Environment.Exit(1);
            }

            //Launch the server
            Server = new SocketServer(Localhost,Localport);
            Client = new SocketClient(Remotehost,Remoteport);
            Server.ReceivedDataHandler += ServerReceivedDataHandler;
            Client.ReceivedDataHandler += ClientReceivedDataHandler;
        }

        private static void ServerReceivedDataHandler(byte[] data, TcpClient client)
        {
            if (!Options.HasFlag(optionflags.READONLY))
            {
                Client.Broadcast(data);
                new System.Threading.Thread(() =>
                                                {
                                                    if (Options.HasFlag(optionflags.VERBOSE))
                                                        Console.WriteLine(HexView(data));
                                                }).Start();
            }
        }

        private static void ClientReceivedDataHandler(byte[] data, TcpClient client)
        {
            if (!Options.HasFlag(optionflags.WRITEONLY))
            {
                Server.Broadcast(data);
                new System.Threading.Thread(() =>
                                                {
                                                    if (Options.HasFlag(optionflags.VERBOSE))
                                                        Console.WriteLine(HexView(data));
                                                }).Start();
            }
        }

        [Flags]
        public enum optionflags
        {
            SHOWHELP = 1,
            VERBOSE = 2,
            PROMPT = 4,
            READONLY = 8,
            WRITEONLY = 16
        }

        private static OptionSet p = new OptionSet()
                                         {
                                             {"h|host=", "Remote Host", v => Remotehost = v},
                                             {"p|port=", "Remote host port", v => Remoteport = Localport = int.Parse(v)},
                                             {"a|localhost=", "Local IP Address to listen on (0.0.0.0)", v => Localhost = IPAddress.Parse(v)},
                                             {"l|localport=", "Local port to use (defaults to remote port)", v => Localport = int.Parse(v)},
                                             {"v|verbose", "Show traffic in hex/ascii", v => Options |= optionflags.VERBOSE},
                                             {"read-only", "Only allow clients to consume traffic", v => Options |= optionflags.READONLY},
                                             {"write-only", "Only allow clients to send traffic", v => Options |= optionflags.WRITEONLY},
                                             {"prompt", "Prompt for host/port", v => Options |= optionflags.PROMPT},
                                             {"?|help", "Show this help documentation", v => Options |= optionflags.SHOWHELP}
                                         };

        private static void ShowHelp(string err = null)
        {
            Console.WriteLine("  Usage: {0} -h (host) -p (port) [options]",
                              Path.GetFileName(Environment.GetCommandLineArgs().First()));
            if (!String.IsNullOrEmpty(err))
                Console.WriteLine("    {0}", err);
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        public static string HexView(byte[] Data)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder text = new StringBuilder();
            StringBuilder result = new StringBuilder();
            char[] ch = new char[1];
            for (int x = 0; x < Data.Length; x += 16)
            {
                text.Length = 0;
                sb.Length = 0;
                for (int y = 0; y < 16; ++y)
                {
                    if ((x + y) > (Data.Length - 1))
                        break;
                    ch[0] = (char)Data[x + y];
                    sb.AppendFormat("{0,0:X2} ", (int)ch[0]);
                    if (((int)ch[0] < 32) || ((int)ch[0] > 127))
                        ch[0] = '.';
                    text.Append(ch);
                }
                text.Append("\r\n");
                while (sb.Length < 52)
                    sb.Append(" ");
                sb.Append(text.ToString());
                result.Append(sb.ToString());
            }
            return result.ToString();
        }
    }
}
