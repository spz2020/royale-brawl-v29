namespace Supercell.Laser.Server.Networking
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Supercell.Laser.Server.Networking.Session;
    using Supercell.Laser.Server.Settings;

    public static class TCPGateway
    {
        private static List<Connection> ActiveConnections;
        private static Socket Socket;
        private static Thread Thread;
        private static ManualResetEvent AcceptEvent;
        private static Dictionary<IPAddress, int> ConnectionAttempts;
        private static HashSet<IPAddress> IPBlacklist;
        private static HashSet<IPAddress> LoggedBlacklistedIPs;
        private static string BlacklistFilePath = "ipblacklist.txt";
        private static bool AntiDDoSEnabled;
        private static Timer BlacklistReloadTimer;

        public static void Init(string host, int port)
        {
            ActiveConnections = new List<Connection>();
            ConnectionAttempts = new Dictionary<IPAddress, int>();
            IPBlacklist = new HashSet<IPAddress>();
            LoggedBlacklistedIPs = new HashSet<IPAddress>();

            Configuration config = Configuration.LoadFromFile("config.json");
            AntiDDoSEnabled = config.antiddos;

            if (AntiDDoSEnabled)
            {
                LoadBlacklist();
                // reload blacklist every 60 seconds
                BlacklistReloadTimer = new Timer(ReloadBlacklist, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
            }

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
            Socket.Listen(10000000);

            AcceptEvent = new ManualResetEvent(false);

            Thread = new Thread(TCPGateway.Update);
            Thread.Start();

            Logger.Print($"TCP Server started at {host}:{port}");
        }

        private static void Update()
        {
            while (true)
            {
                AcceptEvent.Reset();
                Socket.BeginAccept(new AsyncCallback(OnAccept), null);
                AcceptEvent.WaitOne();
            }
        }

        private static void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket client = Socket.EndAccept(ar);
                IPAddress clientIP = ((IPEndPoint)client.RemoteEndPoint).Address;

                if (AntiDDoSEnabled)
                {
                    lock (IPBlacklist)
                    {
                        if (IPBlacklist.Contains(clientIP))
                        {
                            if (!LoggedBlacklistedIPs.Contains(clientIP))
                            {
                                Logger.Print($"Blocked connection from blacklisted IP: {clientIP}");
                                LoggedBlacklistedIPs.Add(clientIP);
                            }
                            client.Close();
                            AcceptEvent.Set();
                            return;
                        }

                        if (!ConnectionAttempts.ContainsKey(clientIP))
                        {
                            ConnectionAttempts[clientIP] = 0;
                        }

                        ConnectionAttempts[clientIP]++;

                        if (ConnectionAttempts[clientIP] > 6)
                        {
                            Logger.Print($"DDoS from {clientIP} detected. Has been added to blacklist.");
                            IPBlacklist.Add(clientIP);
                            File.AppendAllText(BlacklistFilePath, clientIP + Environment.NewLine);
                            client.Close();
                            AcceptEvent.Set();
                            return;
                        }
                    }
                }

                Connection connection = new Connection(client);
                ActiveConnections.Add(connection);
                Logger.Print($"New connection from IP: {clientIP}!");
                Connections.AddConnection(connection);
                client.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(OnReceive), connection);
            }
            catch (Exception)
            {
                ;
            }

            AcceptEvent.Set();
        }

        private static void OnReceive(IAsyncResult ar)
        {
            Connection connection = (Connection)ar.AsyncState;
            if (connection == null) return;

            try
            {
                int r = connection.Socket.EndReceive(ar);
                if (r <= 0)
                {
                    if (connection.Socket.Connected)
                    {
                        IPAddress clientIP = ((IPEndPoint)connection.Socket.RemoteEndPoint).Address;
                        Logger.Print($"Client with IP {clientIP} disconnected.");
                    }
                    ActiveConnections.Remove(connection);
                    if (connection.MessageManager.HomeMode != null)
                    {
                        Sessions.Remove(connection.Avatar.AccountId);
                    }
                    connection.Close();
                    return;
                }

                connection.Memory.Write(connection.ReadBuffer, 0, r);
                if (connection.Messaging.OnReceive() != 0)
                {
                    if (connection.Socket.Connected)
                    {
                        IPAddress clientIP = ((IPEndPoint)connection.Socket.RemoteEndPoint).Address;
                        Logger.Print($"Client with IP {clientIP} disconnected.");
                    }
                    ActiveConnections.Remove(connection);
                    if (connection.MessageManager.HomeMode != null)
                    {
                        Sessions.Remove(connection.Avatar.AccountId);
                    }
                    connection.Close();
                    return;
                }
                connection.Socket.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(OnReceive), connection);
            }
            catch (SocketException)
            {
                if (connection.Socket.Connected)
                {
                    IPAddress clientIP = ((IPEndPoint)connection.Socket.RemoteEndPoint).Address;
                    Logger.Print($"Client with IP {clientIP} disconnected.");
                }
                ActiveConnections.Remove(connection);
                if (connection.MessageManager.HomeMode != null)
                {
                    Sessions.Remove(connection.Avatar.AccountId);
                }
                connection.Close();
            }
            catch (Exception)
            {
                connection.Close();
            }
        }

        public static void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndSend(ar);
            }
            catch (Exception)
            {
                ;
            }
        }

        private static void LoadBlacklist()
        {
            if (File.Exists(BlacklistFilePath))
            {
                lock (IPBlacklist)
                {
                    IPBlacklist.Clear();
                    LoggedBlacklistedIPs.Clear();
                    foreach (var line in File.ReadAllLines(BlacklistFilePath))
                    {
                        if (IPAddress.TryParse(line, out var ip))
                        {
                            IPBlacklist.Add(ip);
                        }
                    }
                }
            }
        }

        private static void ReloadBlacklist(object state)
        {
            Logger.Print("Reloading blacklist...");
            LoadBlacklist();
            Logger.Print("Blacklist has been reloaded.");
        }
    }
}