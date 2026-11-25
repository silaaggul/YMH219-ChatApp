using System.Net;
using System.Net.Sockets;
using ChatCommon;

namespace ChatServer;

public sealed class ChatServerHost
{
    private readonly List<ClientSession> _clients = new();
    private readonly object _gate = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _listener is not null;
    public IReadOnlyList<ClientSession> Clients
    {
        get
        {
            lock (_gate)
            {
                return _clients.ToList();
            }
        }
    }

    public event Action<string>? Log;
    public event Action? ClientsChanged;

    public async Task StartAsync(int port)
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        RaiseLog($"Sunucu {port} portunda dinlemede.");
        _ = AcceptLoopAsync(_cts.Token);
        await Task.CompletedTask;
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        _listener = null;

        lock (_gate)
        {
            foreach (ClientSession client in _clients.ToList())
            {
                client.Dispose();
            }

            _clients.Clear();
        }

        ClientsChanged?.Invoke();
        RaiseLog("Sunucu durduruldu.");
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener is not null)
        {
            try
            {
                TcpClient tcp = await _listener.AcceptTcpClientAsync(token);
                var session = new ClientSession(tcp);
                _ = HandleClientAsync(session, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                RaiseLog("Kabul hatası: " + ex.Message);
            }
        }
    }

    private async Task HandleClientAsync(ClientSession session, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && session.Tcp.Connected)
            {
                string? line = await session.Reader.ReadLineAsync(token);
                if (line is null)
                {
                    break;
                }

                await ProcessLineAsync(session, line);
            }
        }
        catch (Exception ex)
        {
            RaiseLog($"{session.Username} bağlantı hatası: {ex.Message}");
        }
        finally
        {
            Remove(session);
        }
    }

    private async Task ProcessLineAsync(ClientSession session, string line)
    {
        if (line.StartsWith("JOIN|", StringComparison.Ordinal))
        {
            session.Username = line[5..].Trim();
            if (string.IsNullOrWhiteSpace(session.Username))
            {
                session.Username = "Anonim";
            }

            Add(session);
            var joined = new ChatMessage { Sender = "Sistem", Text = $"{session.Username} odaya katıldı." };
            await BroadcastAsync(joined.ToWire());
            return;
        }

        if (line == "LEAVE")
        {
            session.Dispose();
            return;
        }

        if (line.StartsWith("MSG|", StringComparison.Ordinal))
        {
            var message = new ChatMessage { Sender = session.Username, Text = line[4..] };
            await BroadcastAsync(message.ToWire());
            AppendHistory(message);
            return;
        }

        if (line.StartsWith("PRIV|", StringComparison.Ordinal))
        {
            string[] parts = line.Split('|', 3);
            if (parts.Length < 3)
            {
                return;
            }

            string target = parts[1];
            var message = new ChatMessage
            {
                Sender = session.Username,
                Text = $"(-> {target}) {parts[2]}",
                IsPrivate = true
            };
            await SendToAsync(session.Username, message.ToWire());
            await SendToAsync(target, message.ToWire());
        }
    }

    private void Add(ClientSession session)
    {
        lock (_gate)
        {
            _clients.Add(session);
        }

        ClientsChanged?.Invoke();
        RaiseLog($"{session.Username} bağlandı ({session.Endpoint}).");
    }

    private void Remove(ClientSession session)
    {
        lock (_gate)
        {
            _clients.Remove(session);
        }

        session.Dispose();
        ClientsChanged?.Invoke();
        RaiseLog($"{session.Username} ayrıldı.");
        var left = new ChatMessage { Sender = "Sistem", Text = $"{session.Username} ayrıldı." };
        _ = BroadcastAsync(left.ToWire());
    }

    private async Task BroadcastAsync(string wire)
    {
        List<ClientSession> snapshot;
        lock (_gate)
        {
            snapshot = _clients.ToList();
        }

        foreach (ClientSession client in snapshot)
        {
            try
            {
                await client.SendLineAsync(wire);
            }
            catch
            {
                // kopan istemci Accept döngüsünde temizlenir
            }
        }
    }

    private async Task SendToAsync(string username, string wire)
    {
        List<ClientSession> snapshot;
        lock (_gate)
        {
            snapshot = _clients.Where(c => c.Username.Equals(username, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        foreach (ClientSession client in snapshot)
        {
            try
            {
                await client.SendLineAsync(wire);
            }
            catch
            {
            }
        }
    }

    private void AppendHistory(ChatMessage message)
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "chat-history.txt");
            File.AppendAllText(path, message + Environment.NewLine);
        }
        catch
        {
        }
    }

    private void RaiseLog(string text) => Log?.Invoke($"[{DateTime.Now:HH:mm:ss}] {text}");
}
