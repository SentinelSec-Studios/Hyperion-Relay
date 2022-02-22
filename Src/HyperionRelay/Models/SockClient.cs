using System.Net;
using System.Net.Sockets;

namespace HyperionRelay.Models
{
    /// <summary>
    /// Represents a connected client
    /// </summary>
    public class SockClient
    {
        /// <summary>
        /// The GUID for the client in context.
        /// </summary>
        public Guid? ClientGuid { get; set; }

        /// <summary>
        /// The TCPClient object for the client in context.
        /// </summary>
        public TcpClient? TcpClient { get; set; }

        /// <summary>
        /// The network stream object for the client in context.
        /// </summary>
        public NetworkStream? Stream { get; set; }

        /// <summary>
        /// Ease of use method for obtaining the Server Endpoint.
        /// </summary>
        /// <returns>Endpoint</returns>
        public EndPoint? GetServerIPAddress()
        {
            return Stream!.Socket.LocalEndPoint;
        }

        /// <summary>
        /// Ease of use method for obtaining the Client Endpoint.
        /// </summary>
        /// <returns>Endpoint</returns>
        public EndPoint? GetIPAddress()
        {
            return Stream!.Socket.RemoteEndPoint;
        }

        /// <summary>
        /// Disconnects the client in context.
        /// </summary>
        public void Disconnect()
        {
            Stream!.Socket.Close();
        }
    }
}
