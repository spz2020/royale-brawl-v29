namespace Supercell.Laser.Server.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Supercell.Laser.Server.Networking.Session;

    public static class TCPGateway
    {
        private static readonly List<Connection> ActiveConnections = new List<Connection>();
        private static readonly Dictionary<string, int> IpConnections = new Dictionary<string, int>();
        private static readonly HashSet<string> RejectedIps = new HashSet<string>();
        private const int MaxConnectionsPerIp = 5;

        private static Socket _socket;
        private static Thread _thread;
        private static ManualResetEvent _acceptEvent;

        public static void Init(string host, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
            _socket.Listen(10000000);

            _acceptEvent = new ManualResetEvent(false);

            _thread = new Thread(Update);
            _thread.Start();

            Logger.Print($"TCP Server started at {host}:{port}");
        }

        private static void Update()
        {
            while (true)
            {
                _acceptEvent.Reset();
                _socket.BeginAccept(OnAccept, null);
                _acceptEvent.WaitOne();
            }
        }

        private static void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket client = _socket.EndAccept(ar);
                string clientIp = ((IPEndPoint)client.RemoteEndPoint).Address.ToString();

                lock (IpConnections)
                {
                    if (!IpConnections.ContainsKey(clientIp))
                    {
                        IpConnections[clientIp] = 1;
                    }
                    else if (IpConnections[clientIp] >= MaxConnectionsPerIp)
                    {
                        if (!RejectedIps.Contains(clientIp))
                        {
                            lock (RejectedIps)
                            {
                                RejectedIps.Add(clientIp);
                                Logger.Print($"DDOS attempt from {clientIp} has been prevented");
                            }
                        }
                        client.Close();
                        _acceptEvent.Set();
                        return;
                    }
                    else
                    {
                        IpConnections[clientIp]++;
                    }
                }

                Connection connection = new Connection(client);
                ActiveConnections.Add(connection);
                Logger.Print($"New connection from {clientIp}");
                Connections.AddConnection(connection);
                client.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, OnReceive, connection);
            }
            catch (Exception ex)
            {
                Logger.Print($"Error accepting connection: {ex.Message}");
            }

            _acceptEvent.Set();
        }

        private static void OnReceive(IAsyncResult ar)
        {
            Connection connection = (Connection)ar.AsyncState;
            if (connection == null) return;

            try
            {
                int bytesRead = connection.Socket.EndReceive(ar);
                if (bytesRead <= 0)
                {
                    Logger.Print("Client disconnected.");
                    RemoveIpConnection(connection.Socket);
                    ActiveConnections.Remove(connection);
                    if (connection.MessageManager.HomeMode != null)
                    {
                        Sessions.Remove(connection.Avatar.AccountId);
                    }
                    connection.Close();
                    return;
                }

                connection.Memory.Write(connection.ReadBuffer, 0, bytesRead);
                if (connection.Messaging.OnReceive() != 0)
                {
                    RemoveIpConnection(connection.Socket);
                    ActiveConnections.Remove(connection);
                    if (connection.MessageManager.HomeMode != null)
                    {
                        Sessions.Remove(connection.Avatar.AccountId);
                    }
                    connection.Close();
                    Logger.Print("Client disconnected.");
                    return;
                }
                connection.Socket.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, OnReceive, connection);
            }
            catch (SocketException ex)
            {
                Logger.Print($"Socket error: {ex.Message}");
                RemoveIpConnection(connection.Socket);
                ActiveConnections.Remove(connection);
                if (connection.MessageManager.HomeMode != null)
                {
                    Sessions.Remove(connection.Avatar.AccountId);
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                Logger.Print($"Unhandled exception: {ex.Message}, trace: {ex.StackTrace}");
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
            catch (Exception ex)
            {
                Logger.Print($"Error sending data: {ex.Message}");
            }
        }

        private static void RemoveIpConnection(Socket socket)
        {
            try
            {
                string clientIp = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
                lock (IpConnections)
                {
                    if (IpConnections.ContainsKey(clientIp))
                    {
                        IpConnections[clientIp]--;
                        if (IpConnections[clientIp] <= 0)
                        {
                            IpConnections.Remove(clientIp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Print($"Error removing IP connection: {ex.Message}");
            }
        }
    }
}