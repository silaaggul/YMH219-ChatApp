using System.Net.Sockets;

namespace ChatServer;

public sealed class ClientSession : IDisposable
{
    public TcpClient Tcp { get; }
    public string Username { get; set; } = "Anonim";
    public string Endpoint { get; }
    public StreamReader Reader { get; }
    public StreamWriter Writer { get; }

    public ClientSession(TcpClient tcp)
    {
        Tcp = tcp;
        Endpoint = tcp.Client.RemoteEndPoint?.ToString() ?? "-";
        NetworkStream stream = tcp.GetStream();
        Reader = new StreamReader(stream);
        Writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public async Task SendLineAsync(string line)
    {
        await Writer.WriteLineAsync(line);
    }

    public void Dispose()
    {
        try { Reader.Dispose(); } catch { }
        try { Writer.Dispose(); } catch { }
        try { Tcp.Close(); } catch { }
    }
}
