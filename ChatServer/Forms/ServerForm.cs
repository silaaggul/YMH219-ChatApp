using ChatCommon;

namespace ChatServer.Forms;

public sealed class ServerForm : Form
{
    private readonly ChatServerHost _host = new();
    private readonly ListBox _logList;
    private readonly ListBox _clientList;
    private readonly NumericUpDown _portBox;
    private readonly Button _startButton;
    private readonly Button _stopButton;
    private readonly Label _statusLabel;

    public ServerForm()
    {
        Text = "YMH219 Chat Sunucu";
        ClientSize = new Size(780, 480);
        StartPosition = FormStartPosition.CenterScreen;

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Padding = new Padding(10, 6, 10, 6),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Text = "Durum: Kapalı"
        };

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8) };
        top.Controls.Add(new Label { Text = "Port:", AutoSize = true, Margin = new Padding(4, 10, 4, 0) });
        _portBox = new NumericUpDown { Minimum = 1024, Maximum = 65535, Value = Protocol.DefaultPort, Width = 80 };
        _startButton = new Button { Text = "Sunucuyu Başlat", Width = 130, Height = 32 };
        _stopButton = new Button { Text = "Durdur", Width = 90, Height = 32, Enabled = false };
        _startButton.Click += async (_, _) => await StartAsync();
        _stopButton.Click += (_, _) => StopServer();
        top.Controls.Add(_portBox);
        top.Controls.Add(_startButton);
        top.Controls.Add(_stopButton);

        _clientList = new ListBox { Dock = DockStyle.Right, Width = 240 };
        _logList = new ListBox { Dock = DockStyle.Fill };

        var clientsHeader = new Label
        {
            Dock = DockStyle.Right,
            Width = 240,
            Height = 24,
            Text = "Bağlı istemciler",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        Controls.Add(_logList);
        Controls.Add(_clientList);
        Controls.Add(top);
        Controls.Add(_statusLabel);

        _host.Log += line => BeginInvoke(() =>
        {
            _logList.Items.Add(line);
            _logList.TopIndex = _logList.Items.Count - 1;
        });
        _host.ClientsChanged += () => BeginInvoke(RefreshClients);

        FormClosing += (_, _) => _host.Stop();
    }

    private async Task StartAsync()
    {
        try
        {
            await _host.StartAsync((int)_portBox.Value);
            _startButton.Enabled = false;
            _stopButton.Enabled = true;
            _statusLabel.Text = $"Durum: Dinlemede (port {(int)_portBox.Value})";
            _statusLabel.BackColor = Color.FromArgb(198, 239, 206);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Sunucu başlatılamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StopServer()
    {
        _host.Stop();
        _startButton.Enabled = true;
        _stopButton.Enabled = false;
        _statusLabel.Text = "Durum: Kapalı";
        _statusLabel.BackColor = Color.FromArgb(255, 220, 220);
        RefreshClients();
    }

    private void RefreshClients()
    {
        _clientList.Items.Clear();
        foreach (ClientSession client in _host.Clients)
        {
            _clientList.Items.Add($"{client.Username}  [{client.Endpoint}]");
        }
    }
}
