using System.Net;
using System.Net.Sockets;
using Supercell.Laser.Server.Networking.Session;
using Supercell.Laser.Server.Settings;

namespace Supercell.Laser.Server.Networking
{
    public static class IPBlacklist
    {
        private static readonly string BlacklistFilePath = "ipblacklist.txt";
        private static readonly object FileLock = new object();
        private static HashSet<string> BlockedIPs = new HashSet<string>();
        private static FileSystemWatcher FileWatcher;

        public static void Initialize()
        {
            LoadBlacklist();
            SetupFileWatcher();
        }

        public static void LoadBlacklist()
        {
            lock (FileLock)
            {
                if (!File.Exists(BlacklistFilePath))
                {
                    File.Create(BlacklistFilePath).Close();
                    Logger.Print("Created ipblacklist.txt file.");
                }

                BlockedIPs = File.ReadAllLines(BlacklistFilePath)
                    .Where(ip => !string.IsNullOrWhiteSpace(ip))
                    .ToHashSet();

                Logger.Print($"Loaded {BlockedIPs.Count} IPs from ipblacklist.txt.");
            }
        }

        public static void SetupFileWatcher()
        {
            FileWatcher = new FileSystemWatcher
            {
                Path = Directory.GetCurrentDirectory(),
                Filter = BlacklistFilePath,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            FileWatcher.Changed += OnBlacklistFileChanged;
            FileWatcher.Created += OnBlacklistFileChanged;
            FileWatcher.Deleted += OnBlacklistFileChanged;
            FileWatcher.Renamed += OnBlacklistFileChanged;

            FileWatcher.EnableRaisingEvents = true;
            Logger.Print("File watcher for ipblacklist.txt is active.");
        }

        private static void OnBlacklistFileChanged(object sender, FileSystemEventArgs e)
        {
            Logger.Print($"ipblacklist.txt file changed. Reloading blacklist...");
            LoadBlacklist();
        }

        public static void BlockIP(string ip)
        {
            lock (FileLock)
            {
                if (BlockedIPs.Add(ip))
                {
                    File.AppendAllLines(BlacklistFilePath, new[] { ip });
                    Logger.Print($"IP {ip} added to blacklist and saved to ipblacklist.txt.");
                }
            }
        }

        public static bool IsIPBlocked(string ip)
        {
            return BlockedIPs.Contains(ip);
        }
    }

    public static class TCPGateway
    {
        private static List<Connection> ActiveConnections = new List<Connection>();
        private static Dictionary<string, PacketCounter> PacketCounters = new Dictionary<string, PacketCounter>();
        private static Dictionary<string, ConnectionAttemptCounter> ConnectionAttempts = new Dictionary<string, ConnectionAttemptCounter>();
        private static Dictionary<string, DateTime> LastLogTimes = new Dictionary<string, DateTime>();

        private static Socket Socket;
        private static Thread Thread;
        private static Timer CleanupTimer;

        private static ManualResetEvent AcceptEvent = new ManualResetEvent(false);
        private static readonly object ConnectionLock = new object();

        public static void Init(string host, int port)
        {
            if (Configuration.Instance.antiddos)
            {
                IPBlacklist.Initialize();
            }

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
            Socket.Listen(99999999);

            Thread = new Thread(Update);
            Thread.Start();

            CleanupTimer = new Timer(CleanupInactiveConnections, null, 10000, 10000);

            Logger.Print($"TCP server started on {host}:{port}");
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
                string clientIp = ((IPEndPoint)client.RemoteEndPoint).Address.ToString();

                if (Configuration.Instance.antiddos && IPBlacklist.IsIPBlocked(clientIp))
                {
                    LogIfNeeded(clientIp, $"Rejected: IP {clientIp} is blocked.");
                    client.Close();
                    AcceptEvent.Set();
                    return;
                }

                if (Configuration.Instance.antiddos)
                {
                    HandleConnectionAttempt(clientIp);
                }

                Connection connection = new Connection(client);
                lock (ConnectionLock)
                {
                    ActiveConnections.Add(connection);
                }
                Logger.Print($"New connection from {clientIp}");

                Connections.AddConnection(connection);
                client.BeginReceive(
                    connection.ReadBuffer,
                    0,
                    1024,
                    SocketFlags.None,
                    new AsyncCallback(OnReceive),
                    connection
                );
            }
            catch (Exception ex)
            {
                Logger.Print($"Error accepting connection: {ex.Message}");
            }
            finally
            {
                AcceptEvent.Set();
            }
        }

        private static void HandleConnectionAttempt(string clientIp)
        {
            if (!ConnectionAttempts.ContainsKey(clientIp))
            {
                ConnectionAttempts[clientIp] = new ConnectionAttemptCounter();
            }

            var attemptCounter = ConnectionAttempts[clientIp];
            attemptCounter.AttemptCount++;
            if ((DateTime.Now - attemptCounter.FirstAttemptTime).TotalSeconds > 10)
            {
                attemptCounter.FirstAttemptTime = DateTime.Now;
                attemptCounter.AttemptCount = 1;
            }

            if (attemptCounter.AttemptCount > 4)
            {
                IPBlacklist.BlockIP(clientIp);
                Logger.Print($"IP {clientIp} banned for too many connection attempts.");
            }
        }

        private static void OnReceive(IAsyncResult ar)
        {
            Connection connection = (Connection)ar.AsyncState;
            if (connection == null || connection.Socket == null || !connection.Socket.Connected)
                return;

            string clientIp = ((IPEndPoint)connection.Socket.RemoteEndPoint).Address.ToString();

            try
            {
                int bytesRead = connection.Socket.EndReceive(ar);
                if (bytesRead <= 0)
                {
                    Logger.Print($"{clientIp} disconnected.");
                    RemoveConnection(connection);
                    return;
                }

                if (Configuration.Instance.antiddos)
                {
                    HandlePacketCounter(clientIp);
                }

                connection.Memory.Write(connection.ReadBuffer, 0, bytesRead);
                connection.UpdateLastActiveTime();

                if (connection.Messaging.OnReceive() != 0)
                {
                    RemoveConnection(connection);
                    Logger.Print($"{clientIp} disconnected.");
                    return;
                }
                connection.Socket.BeginReceive(
                    connection.ReadBuffer,
                    0,
                    1024,
                    SocketFlags.None,
                    new AsyncCallback(OnReceive),
                    connection
                );
            }
            catch (ObjectDisposedException)
            {
                Logger.Print($"Client socket {clientIp} was already closed.");
            }
            catch (SocketException)
            {
                RemoveConnection(connection);
                Logger.Print($"{clientIp} disconnected due to socket error.");
            }
            catch (Exception ex)
            {
                Logger.Print($"Unexpected error from {clientIp}: {ex.Message}");
                RemoveConnection(connection);
            }
        }

        private static void HandlePacketCounter(string clientIp)
        {
            if (!PacketCounters.ContainsKey(clientIp))
            {
                PacketCounters[clientIp] = new PacketCounter();
            }

            var counter = PacketCounters[clientIp];
            counter.PacketCount++;
            if ((DateTime.Now - counter.FirstPacketTime).TotalSeconds > 10)
            {
                counter.FirstPacketTime = DateTime.Now;
                counter.PacketCount = 1;
            }

            if (counter.PacketCount > 50)
            {
                IPBlacklist.BlockIP(clientIp);
                Logger.Print($"IP {clientIp} banned for exceeding packet limit.");
            }
        }

        private static void CleanupInactiveConnections(object state)
        {
            DateTime now = DateTime.Now;
            var connectionsToRemove = new List<Connection>();

            lock (ConnectionLock)
            {
                for (int i = ActiveConnections.Count - 1; i >= 0; i--)
                {
                    var connection = ActiveConnections[i];

                    if (connection != null && connection.Socket != null)
                    {
                        if (connection.Socket.Connected)
                        {
                            if ((now - connection.LastActiveTime).TotalSeconds > 120)
                            {
                                Logger.Print(
                                    $"Closing inactive connection from {connection.Socket.RemoteEndPoint}."
                                );
                                connectionsToRemove.Add(connection);
                            }
                        }
                        else
                        {
                            Logger.Print($"Socket is already closed and will be removed.");
                            connectionsToRemove.Add(connection);
                        }
                    }
                    else
                    {
                        Logger.Print("Connection or its socket is null, removing.");
                        connectionsToRemove.Add(connection);
                    }
                }

                foreach (var conn in connectionsToRemove)
                {
                    ActiveConnections.Remove(conn);
                    conn.Close();
                }
            }
        }

        private static void RemoveConnection(Connection connection)
        {
            lock (ConnectionLock)
            {
                if (ActiveConnections.Contains(connection))
                {
                    ActiveConnections.Remove(connection);
                }
                connection.Close();
                if (connection.MessageManager.HomeMode != null)
                {
                    Sessions.Remove(connection.Avatar.AccountId);
                }
            }
        }

        private static void LogIfNeeded(string clientIp, string message)
        {
            if (
                !LastLogTimes.ContainsKey(clientIp)
                || (DateTime.Now - LastLogTimes[clientIp]).TotalSeconds >= 10
            )
            {
                Logger.Print(message);
                LastLogTimes[clientIp] = DateTime.Now;
            }
        }

        public static void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Logger.Print($"Error sending data: {ex.Message}");
            }
        }
    }

    public class PacketCounter
    {
        public DateTime FirstPacketTime { get; set; }
        public int PacketCount { get; set; }

        public PacketCounter()
        {
            FirstPacketTime = DateTime.Now;
            PacketCount = 0;
        }
    }

    public class ConnectionAttemptCounter
    {
        public DateTime FirstAttemptTime { get; set; }
        public int AttemptCount { get; set; }

        public ConnectionAttemptCounter()
        {
            FirstAttemptTime = DateTime.Now;
            AttemptCount = 0;
        }
    }
}