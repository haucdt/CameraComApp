using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CameraComApp
{
    public class SocketService
    {
        private TcpListener _server;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private CancellationTokenSource _cancellationTokenSource;
        public event EventHandler<string> CommandReceived;

        public void StartServer(int port)
        {
            StopServer();
            try
            {
                _server = new TcpListener(IPAddress.Any, port);
                _server.Start();
                _cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => AcceptClients(_cancellationTokenSource.Token));
                Console.WriteLine($"Socket server started on port {port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        public void StopServer()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    client.Close();
                   // client.Dispose();
                }
                _clients.Clear();
            }

            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }
        }

        public void SendData(string data)
        {
            lock (_clients)
            {
                List<TcpClient> disconnectedClients = new List<TcpClient>();
                foreach (var client in _clients)
                {
                    try
                    {
                        if (client.Connected)
                        {
                            NetworkStream stream = client.GetStream();
                            byte[] buffer = Encoding.ASCII.GetBytes(data + "\n");
                            stream.Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            disconnectedClients.Add(client);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending data to client: {ex.Message}");
                        disconnectedClients.Add(client);
                    }
                }

                foreach (var client in disconnectedClients)
                {
                    _clients.Remove(client);
                    client.Close();
                   // client.Dispose();
                }
            }
        }

        private async Task AcceptClients(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    lock (_clients)
                    {
                        _clients.Add(client);
                    }
                    Task.Run(() => HandleClient(client, cancellationToken));
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                    Console.WriteLine($"Error accepting clients: {ex.Message}");
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    while (client.Connected && !cancellationToken.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead > 0)
                        {
                            string data = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                            CommandReceived?.Invoke(this, data);
                        }
                        else
                        {
                            break; // Client disconnected
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                lock (_clients)
                {
                    _clients.Remove(client);
                    client.Close();
                   // client.Dispose();
                }
            }
        }
    }
}