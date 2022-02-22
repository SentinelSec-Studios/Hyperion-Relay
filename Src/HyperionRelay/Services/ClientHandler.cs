using HyperionRelay.Enums;
using HyperionRelay.Exceptions;
using HyperionRelay.Infrastructure;
using HyperionRelay.Models;
using HyperionRelay.Startup;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace HyperionRelay.ClientManagement
{
    class ClientHandler
    {
        /// <summary>
        /// Spawns a task to handle a connected client's session.
        /// </summary>
        /// <param name="client"></param>
        public static async void HandleClientAsync(TcpClient client)
        {
            await Task.Run(async () =>
            {
                // Set initial client variables.
                Guid clientID = Guid.NewGuid();
                var stream = client.GetStream();
                // Here we generate a SockClient object and assign a client-guid for identification purposes.
                SockClient? sockClient = new()
                {
                    ClientGuid = clientID,
                    TcpClient = client,
                    Stream = client.GetStream()
                };
                // Add the initialized SockClient object to the dictionary of connected clients.
                RelayStartup.CurrentConfig.CurrentConnectionHandler!.ClientList.TryAdd(clientID, sockClient);
                // Here we intialize a session for the connected client to hold client specific data (clientToken, supported encryption methods, etc.).
                // Additionally we pass the client GUID and IP address for later identification when handling client data.
                ClientSession? session = new(clientID);
                session.ClientEndpoint = (sockClient.GetIPAddress() as IPEndPoint)?.Address;
                // Add the initialized ClientSession object to the dictionary of connected client sessions.
                RelayStartup.CurrentConfig.CurrentConnectionHandler.ClientData.TryAdd(clientID, session);
                // Announce the client connection
                Console.WriteLine($"{client.Client.RemoteEndPoint} connected successfully! Session ID: {session.ClientGuid}", LogLevel.Information);
                // Now we enter the client servicing loop to communicate with the client, this loop is broken upon disconnect or if a client's session becomes untrusted.
                try
                {
                    GatewayClient gatewayClient = new("your-server-address", 0000); // Will be dynamic later via load balancing.
                    await gatewayClient.ConnectAsync();
                    while (!RelayStartup.CurrentConfig.CurrentConnectionHandler.IsDisconnected(stream) && !gatewayClient.IsDisconnected())
                    {
                        if (stream == null || gatewayClient.Stream == null)
                            throw new NullReferenceException("A client stream was null.");
                        if (stream.DataAvailable)
                        {
                            await CopyToAsync(stream, gatewayClient.Stream);
                        }
                        if (gatewayClient.Stream.DataAvailable)
                        {
                            await CopyToAsync(gatewayClient.Stream, stream);
                        }
                        else
                        {
                            // Wait for 1/2ms before polling again for a client transaction. Note: will be configurable in a future release.
                            Thread.Sleep(50);
                        }
                    }
                    gatewayClient.Disconnect();
                    gatewayClient.Client?.Dispose();
                    gatewayClient.Stream?.Dispose();
                }
                catch (IOException e)
                {
                    Console.WriteLine($"IOException thrown on disconnect for client: {clientID}!");
                    Console.WriteLine($"Debug: {e.Message}");
                }
                catch (SecurityViolationException e)
                {
                    Console.WriteLine($"A security violation was commited by client: {clientID}! Disconnecting...");
                    Console.WriteLine($"Debug: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An issue occurred for client {clientID}: {e.Message}");
                }
                // Clean up after connection task has exited...
                Console.WriteLine($"(Client Disconnect): Client handler task disposed for client {sockClient.ClientGuid}");
                RelayStartup.CurrentConfig.CurrentConnectionHandler.ClientCount--;
                RelayStartup.CurrentConfig.CurrentConnectionHandler.ClientData.TryRemove(clientID, out _);
                RelayStartup.CurrentConfig.CurrentConnectionHandler.ClientList.TryRemove(clientID, out _);
                sockClient.TcpClient.Dispose(); // Experimental - Needs more testing.
                sockClient.Stream.Dispose(); // Experimental - Needs more testing.

                session = null;
                sockClient = null;
                // [Debug]: Run garbage collection to make sure nothing is being left behind from the client.
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                return Task.CompletedTask;
            });
        }


        private static async Task CopyToAsync(Stream source, Stream destination, int bufferSize = 81920, CancellationToken cancellationToken = default)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                long LastActivity = 0;
                while (LastActivity < 5000)
                {
                    int bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    //Console.WriteLine(bytesRead);
                    if (bytesRead == 0) break;
                    LastActivity += Environment.TickCount64;
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                }
                //Console.WriteLine("broke loop");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

}

