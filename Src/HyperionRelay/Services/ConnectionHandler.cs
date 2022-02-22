using HyperionRelay.ClientManagement;
using HyperionRelay.Enums;
using HyperionRelay.Models;
using HyperionRelay.Startup;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace HyperionRelay.Infrastructure
{
    public class ConnectionHandler : IDisposable
    {
        // Declare our listener and our manually fired event that allows the server to know when to continue upon a succeeded connection.
        private TcpListener? Listener { get; set; }
        private readonly ManualResetEvent tcpClientConnected = new(false);
        // Declare connected client dictionaries to keep track of our connected clients.
        public int ClientCount { get; set; }
        // Keeps track of client connection specific objects.
        public readonly ConcurrentDictionary<Guid, SockClient> ClientList = new();
        // Keeps track of client specific session objects.
        public readonly ConcurrentDictionary<Guid, ClientSession> ClientData = new();
        // Used for disposing of the listener during restarts.
        private bool disposedValue;

        /// <summary>
        /// Initializes a TCP listener to wait for client connections.
        /// </summary>
        /// <param name="endPoint">A valid IPEndPoint to utilize for client connections.</param>
        public async Task StartServer(IPEndPoint endPoint)
        {
            Listener = new TcpListener(endPoint);
            Listener.Start();
            Console.Clear();
            RelayStartup.DisplayBanner();
            //Console.CursorVisible = true;
            while (disposedValue != true)
            {
                Console.Title = $"SentinelSec: Hyperion Relay | Build {HyperionRelay.HyperionVersion} | LogLevel: {RelayStartup.CurrentConfig.LogLevel} | Listening on: {RelayStartup.CurrentConfig.RelayIP}:{RelayStartup.CurrentConfig.RelayPort} | Connected clients: {ClientCount}";
                if (Listener.Pending())
                {
                    DoBeginAcceptTcpClient(Listener);
                }
                else
                {
                    await Task.Delay(600);
                }
            }
        }

        public void ShutdownServer()
        {
            Console.WriteLine("(Shutdown): Disconnecting all clients...");
            foreach (var client in ClientList)
            {
                client.Value.Disconnect();
            }
            // Fire the event to reloop the server handler.
            tcpClientConnected.Set();
            HyperionRelay.ServerShutdownCompleted!.Set();
        }

        /// <summary>
        /// Accepts incoming client connection requests
        /// </summary>
        /// <param name="listener"></param>
        private void DoBeginAcceptTcpClient(TcpListener listener)
        {
            // Set the event to nonsignaled state.
            tcpClientConnected.Reset();

            // Accept the connection.
            // BeginAcceptSocket() creates the accepted socket.
            listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), listener);

            // Wait until a connection is made and processed before continuing.
            tcpClientConnected.WaitOne();
        }

        /// <summary>
        /// Moves a connected client to a TCPClient object and passes client to a separate handler thread.
        /// </summary>
        /// <param name="ar"></param>
        private void DoAcceptTcpClientCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;
            // Get the listener that handles the client request.
            TcpListener listener = (TcpListener)ar.AsyncState;
            // Declare client outside of try block to allow further usage if client has not prematurely disconnected.
            TcpClient client;
            try
            {
                // End the client accept operation.
                client = listener.EndAcceptTcpClient(ar);
            }
            catch (SocketException)
            {
                Console.WriteLine("An attempted connection was prematurely dropped!");
                // An Exception has occurred signal the calling thread to continue.
                tcpClientConnected.Set();
                return;
            }

            // Process the connection here and pass-off the client to the client manager.
            if (ValidateClient(client))
            {
                // Increment global client count.
                RelayStartup.CurrentConfig.CurrentConnectionHandler!.ClientCount++;

                // Next we pass off the client to the designated handler function.
                ClientHandler.HandleClientAsync(client); // Fire and forget void method.
            }
            else
            {
                // If the client counter was full reject the connection and notify console.
                Console.WriteLine($"Incoming connection from: {client.Client.RemoteEndPoint} was rejected! (Client Validation Failed)", LogLevel.Information);
                client.Close();
            }
            // Signal the calling thread to continue.
            tcpClientConnected.Set();
        }

        /// <summary>
        /// Checks the connected client's connection information along with server policies to ensure that client complies with server policies.
        /// </summary>
        /// <returns></returns>
        public static bool ValidateClient(TcpClient client)
        {
            HyperionRelay.CheckForUpdatesAsync().Wait();
            if (RelayStartup.CurrentConfig.CurrentConnectionHandler!.ClientCount >= RelayStartup.CurrentConfig.MaxConnectionCount)
                return false;
            // ToDo: Implement a client IP check here.
            // client.Client.RemoteEndPoint will serve as the IP bannable endpoint.

            return true;
        }


        // Base infrastructure for read/write stream below:

        /// <summary>
        /// Checks the underlying connection to test if a disconnection has occured.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>true if disconnection has occured else will return false if connection is valid.</returns>
        public bool IsDisconnected(NetworkStream stream)
        {
            try
            {
                if (stream.Socket.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buffer = new byte[1];
                    if (stream.Socket.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        //Console.WriteLine("Client Connection has died");
                        return true;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Stream was disposed by another function, return disconnected.
                return true;
            }
            catch (SocketException)
            {
                // Connection was closed or a connection issue has occured.
                return true;
            }
            // All checks have passed, the stream in context is still connected.
            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Listener == null) return;
                    // TODO: dispose managed state (managed objects)
                    Listener.Stop();
                    Listener = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);

        }
    }
}
