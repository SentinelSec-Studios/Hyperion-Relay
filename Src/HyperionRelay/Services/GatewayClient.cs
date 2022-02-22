using HyperionRelay.Exceptions;
using System.Net;
using System.Net.Sockets;

namespace HyperionRelay.Infrastructure
{
    internal class GatewayClient
    {
        internal TcpClient? Client { get; set; }
        internal NetworkStream? Stream { get; set; }
        private string GatewayURI { get; set; }
        private int GatewayPort { get; set; }

        public GatewayClient(string GatewayURI, int GatewayPort)
        {
            this.GatewayURI = GatewayURI;
            this.GatewayPort = GatewayPort;
        }

        public async Task ConnectAsync()
        {
            Client = new();
            if (IPAddress.TryParse(GatewayURI, out IPAddress? ipAddress))
            {
                await Client.ConnectAsync(ipAddress, GatewayPort);
            }
            else
            {
                var ip = Dns.GetHostAddresses(GatewayURI)[0];
                if (ip == null)
                    throw new GatewayUnavaliableException();
                await Client.ConnectAsync(Dns.GetHostAddresses(GatewayURI)[0], GatewayPort);
            }

            Stream = Client.GetStream();
        }

        public void Disconnect()
        {
            if (Client == null)
                throw new NullReferenceException("The supplied client was null!");
            Client.Close();
        }

        /// <summary>
        /// Polls the server stream to check if the connection is invalid.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>True: The stream is in a disconnected state otherwise returns False.</returns>
        public bool IsDisconnected()
        {
            if (Stream == null)
                throw new NullReferenceException("Attempted to check disconnect on a null stream!");
            if (Stream.Socket.Poll(0, SelectMode.SelectRead))
            {
                byte[] buffer = new byte[1];
                try
                {
                    if (Stream.Socket.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        // Client has disconnected.
                        return true;
                    }
                }
                catch (SocketException)
                {
                    // An error was thrown whilst checking the connection.
                    return true;
                }
            }
            // Client is still connected to the server.
            return false;
        }
    }
}
