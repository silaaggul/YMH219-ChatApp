using System.Net.Sockets;
using ChatCommon;

namespace ChatClient;

public sealed class ChatClientService : IDisposable
{
    private TcpClient? _tcp;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _tcp?.Connected == true;
    public string Username { get; private set; } = string.Empty;

    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? StatusChanged;
    public event Action<string>? ErrorOccurred;

    public async Task ConnectAsync(string host, int port, string username)
    {
        if (IsConnected)
        {
            return;
        }

        try
        {
            Username = username.Trim();
            if (string.IsNullOrWhiteSpace(Username))
            {
                throw new InvalidOperationException("Kullanıcı adı boş olamaz.");
            }

            _tcp = new TcpClient();
            await _tcp.ConnectAsync(host, port);
            NetworkStream stream = _tcp.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };
            _cts = new CancellationTokenSource();
            await _writer.WriteLineAsync(Protocol.Join(Username));
            _ = ReceiveLoopAsync(_cts.Token);
            StatusChanged?.Invoke("Bağlandı");
        }
        catch (Exception ex)
        {
            Dispose();
            ErrorOccurred?.Invoke("Bağlantı kurulamadı: " + ex.Message);
            StatusChanged?.Invoke("Bağlı değil");
        }
    }

    public async Task SendAsync(string text)
    {
        if (!IsConnected || _writer is null)
        {
            ErrorOccurred?.Invoke("Önce sunucuya bağlanın.");
            return;
        }

        try
        {
            await _writer.WriteLineAsync(Protocol.Broadcast(text));
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke("Mesaj gönderilemedi: " + ex.Message);
        }
    }

    public async Task SendPrivateAsync(string target, string text)
    {
        if (!IsConnected || _writer is null)
        {
            ErrorOccurred?.Invoke("Önce sunucuya bağlanın.");
            return;
        }

        try
        {
            await _writer.WriteLineAsync(Protocol.Private(target, text));
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke("Özel mesaj gönderilemedi: " + ex.Message);
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_writer is not null && IsConnected)
            {
                await _writer.WriteLineAsync(Protocol.Leave());
            }
        }
        catch
        {
        }
        finally
        {
            Dispose();
            StatusChanged?.Invoke("Bağlı değil");
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _reader is not null)
            {
                string? line = await _reader.ReadLineAsync(token);
                if (line is null)
                {
                    break;
                }

                if (ChatMessage.TryParse(line, out ChatMessage message))
                {
                    MessageReceived?.Invoke(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke("Bağlantı koptu: " + ex.Message);
        }
        finally
        {
            Dispose();
            StatusChanged?.Invoke("Bağlı değil");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try { _reader?.Dispose(); } catch { }
        try { _writer?.Dispose(); } catch { }
        try { _tcp?.Close(); } catch { }
        _reader = null;
        _writer = null;
        _tcp = null;
    }
}
