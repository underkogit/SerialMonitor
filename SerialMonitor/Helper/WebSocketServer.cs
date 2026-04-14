using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SerialMonitor.Helper;

public class WebSocketServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<WebSocket> _connectedClients = new();
    private readonly object _clientsLock = new();

    public event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
    public event EventHandler<WebSocketClientEventArgs>? ClientConnected;
    public event EventHandler<WebSocketClientEventArgs>? ClientDisconnected;

    public WebSocketServer(string prefix = "http://localhost:3080/")
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine($"Server started on {string.Join(", ", _listener.Prefixes)}");

        while (!_cts.Token.IsCancellationRequested)
        {
            var context = await _listener.GetContextAsync();
            _ = Task.Run(() => ProcessRequestAsync(context));
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        if (context.Request.IsWebSocketRequest)
        {
            try
            {
                var wsContext = await context.AcceptWebSocketAsync(null);
                var webSocket = wsContext.WebSocket;

                lock (_clientsLock)
                {
                    _connectedClients.Add(webSocket);
                    Console.WriteLine($"Client connected. Total: {_connectedClients.Count}");
                }

                OnClientConnected(webSocket);
                await HandleWebSocketConnection(webSocket);

                lock (_clientsLock)
                {
                    _connectedClients.Remove(webSocket);
                    Console.WriteLine($"Client disconnected. Remaining: {_connectedClients.Count}");
                }

                OnClientDisconnected(webSocket);
                webSocket.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
        }
        else
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
        }
    }

    private async Task HandleWebSocketConnection(WebSocket webSocket)
    {
        var buffer = new byte[4096];

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Message received: {message}");
                    OnMessageReceived(webSocket, message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Client requested closure");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                break;
            }
        }

        Console.WriteLine($"Connection handler completed for client. State: {webSocket.State}");
    }

    public async Task SendToClientAsync(WebSocket client, string message)
    {
        if (client?.State == WebSocketState.Open)
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);
                Console.WriteLine($"Sent to client: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Client not in Open state. State: {client?.State}");
        }
    }

    public async Task SendToAllAsync(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        List<WebSocket> clientsSnapshot;

        lock (_clientsLock)
        {
            clientsSnapshot = _connectedClients.ToList();
        }

        Console.WriteLine($"Sending to all {clientsSnapshot.Count} clients: {message}");

        var clientsToRemove = new List<WebSocket>();

        foreach (var client in clientsSnapshot)
        {
            if (client.State == WebSocketState.Open)
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                        CancellationToken.None);
                }
                catch
                {
                    clientsToRemove.Add(client);
                }
            }
            else
            {
                clientsToRemove.Add(client);
            }
        }

        if (clientsToRemove.Any())
        {
            lock (_clientsLock)
            {
                foreach (var client in clientsToRemove)
                {
                    _connectedClients.Remove(client);
                }
            }
        }
    }

    public async Task SendToAllExceptAsync(string message, WebSocket excludeClient)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        List<WebSocket> clientsSnapshot;

        lock (_clientsLock)
        {
            clientsSnapshot = _connectedClients.ToList();
        }

        var clientsToRemove = new List<WebSocket>();

        foreach (var client in clientsSnapshot)
        {
            if (client != excludeClient && client.State == WebSocketState.Open)
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                        CancellationToken.None);
                }
                catch
                {
                    clientsToRemove.Add(client);
                }
            }
            else if (client.State != WebSocketState.Open)
            {
                clientsToRemove.Add(client);
            }
        }

        if (clientsToRemove.Any())
        {
            lock (_clientsLock)
            {
                foreach (var client in clientsToRemove)
                {
                    _connectedClients.Remove(client);
                }
            }
        }
    }

    public async Task SendToAllAsync(byte[] data, WebSocketMessageType messageType = WebSocketMessageType.Binary)
    {
        List<WebSocket> clientsSnapshot;

        lock (_clientsLock)
        {
            clientsSnapshot = _connectedClients.ToList();
        }

        var clientsToRemove = new List<WebSocket>();

        foreach (var client in clientsSnapshot)
        {
            if (client.State == WebSocketState.Open)
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(data), messageType, true, CancellationToken.None);
                }
                catch
                {
                    clientsToRemove.Add(client);
                }
            }
            else
            {
                clientsToRemove.Add(client);
            }
        }

        if (clientsToRemove.Any())
        {
            lock (_clientsLock)
            {
                foreach (var client in clientsToRemove)
                {
                    _connectedClients.Remove(client);
                }
            }
        }
    }

    public int GetConnectedClientsCount()
    {
        lock (_clientsLock)
        {
            _connectedClients.RemoveAll(c => c.State != WebSocketState.Open);
            return _connectedClients.Count;
        }
    }

    public List<WebSocket> GetConnectedClients()
    {
        lock (_clientsLock)
        {
            _connectedClients.RemoveAll(c => c.State != WebSocketState.Open);
            return _connectedClients.ToList();
        }
    }

    public async Task CloseClientAsync(WebSocket client, string reason = "Server closing")
    {
        if (client.State == WebSocketState.Open)
        {
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
        }
    }

    public async Task CloseAllClientsAsync(string reason = "Server shutting down")
    {
        List<WebSocket> clients;
        lock (_clientsLock)
        {
            clients = _connectedClients.ToList();
        }

        foreach (var client in clients)
        {
            if (client.State == WebSocketState.Open)
            {
                try
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing client: {ex.Message}");
                }
            }
        }

        lock (_clientsLock)
        {
            _connectedClients.Clear();
        }
    }

    protected virtual void OnMessageReceived(WebSocket client, string message)
    {
        MessageReceived?.Invoke(this, new WebSocketMessageEventArgs(client, message));
    }

    protected virtual void OnClientConnected(WebSocket client)
    {
        ClientConnected?.Invoke(this, new WebSocketClientEventArgs(client));
    }

    protected virtual void OnClientDisconnected(WebSocket client)
    {
        ClientDisconnected?.Invoke(this, new WebSocketClientEventArgs(client));
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();

        ((IDisposable)_listener).Dispose();

        lock (_clientsLock)
        {
            foreach (var client in _connectedClients)
            {
                client.Dispose();
            }

            _connectedClients.Clear();
        }
    }
}

public class WebSocketMessageEventArgs : EventArgs
{
    public WebSocket Client { get; }
    public string Message { get; }

    public WebSocketMessageEventArgs(WebSocket client, string message)
    {
        Client = client;
        Message = message;
    }
}

public class WebSocketClientEventArgs : EventArgs
{
    public WebSocket Client { get; }

    public WebSocketClientEventArgs(WebSocket client)
    {
        Client = client;
    }
}