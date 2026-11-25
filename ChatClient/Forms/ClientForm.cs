using ChatCommon;

namespace ChatClient.Forms;

public sealed class ClientForm : Form
{
    private readonly ChatClientService _client = new();
    private readonly TextBox _hostBox;
    private readonly NumericUpDown _portBox;
    private readonly TextBox _userBox;
    private readonly TextBox _messageBox;
    private readonly TextBox _privateTargetBox;
    private readonly ListBox _chatList;
    private readonly Label _statusLabel;
    private readonly Button _connectButton;
    private readonly Button _disconnectButton;
    private readonly Button _sendButton;
    private readonly Button _privateButton;

    public ClientForm()
    {
        Text = "YMH219 Chat İstemci";
        ClientSize = new Size(720, 520);
        StartPosition = FormStartPosition.CenterScreen;

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(10, 6, 10, 6),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Text = "Durum: Bağlı değil"
        };

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8) };
        _hostBox = new TextBox { Text = "127.0.0.1", Width = 110 };
        _portBox = new NumericUpDown { Minimum = 1024, Maximum = 65535, Value = Protocol.DefaultPort, Width = 80 };
        _userBox = new TextBox { Text = "Sila", Width = 100 };
        _connectButton = new Button { Text = "Bağlan", Width = 90, Height = 30 };
        _disconnectButton = new Button { Text = "Bağlantıyı Kes", Width = 120, Height = 30, Enabled = false };
        _connectButton.Click += async (_, _) => await ConnectAsync();
        _disconnectButton.Click += async (_, _) => await _client.DisconnectAsync();
        top.Controls.Add(new Label { Text = "Host", AutoSize = true, Margin = new Padding(4, 8, 4, 0) });
        top.Controls.Add(_hostBox);
        top.Controls.Add(new Label { Text = "Port", AutoSize = true, Margin = new Padding(4, 8, 4, 0) });
        top.Controls.Add(_portBox);
        top.Controls.Add(new Label { Text = "Kullanıcı", AutoSize = true, Margin = new Padding(4, 8, 4, 0) });
        top.Controls.Add(_userBox);
        top.Controls.Add(_connectButton);
        top.Controls.Add(_disconnectButton);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8) };
        _messageBox = new TextBox { Width = 320 };
        _privateTargetBox = new TextBox { Width = 100, PlaceholderText = "özel alıcı" };
        _sendButton = new Button { Text = "Mesaj Gönder", Width = 120, Height = 30, Enabled = false };
        _privateButton = new Button { Text = "Özel Gönder", Width = 110, Height = 30, Enabled = false };
        _sendButton.Click += async (_, _) => await SendAsync(false);
        _privateButton.Click += async (_, _) => await SendAsync(true);
        _messageBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendAsync(false);
            }
        };
        bottom.Controls.Add(_messageBox);
        bottom.Controls.Add(_sendButton);
        bottom.Controls.Add(_privateTargetBox);
        bottom.Controls.Add(_privateButton);

        _chatList = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };

        Controls.Add(_chatList);
        Controls.Add(bottom);
        Controls.Add(top);
        Controls.Add(_statusLabel);

        _client.MessageReceived += msg => BeginInvoke(() =>
        {
            _chatList.Items.Add(msg.ToString());
            _chatList.TopIndex = _chatList.Items.Count - 1;
        });
        _client.StatusChanged += status => BeginInvoke(() => ApplyStatus(status));
        _client.ErrorOccurred += err => BeginInvoke(() =>
            MessageBox.Show(this, err, "Bağlantı", MessageBoxButtons.OK, MessageBoxIcon.Warning));

        FormClosing += async (_, _) => await _client.DisconnectAsync();
    }

    private async Task ConnectAsync()
    {
        await _client.ConnectAsync(_hostBox.Text.Trim(), (int)_portBox.Value, _userBox.Text);
    }

    private async Task SendAsync(bool isPrivate)
    {
        string text = _messageBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (isPrivate)
        {
            await _client.SendPrivateAsync(_privateTargetBox.Text.Trim(), text);
        }
        else
        {
            await _client.SendAsync(text);
        }

        _messageBox.Clear();
    }

    private void ApplyStatus(string status)
    {
        bool connected = status == "Bağlandı";
        _statusLabel.Text = "Durum: " + status;
        _statusLabel.BackColor = connected ? Color.FromArgb(198, 239, 206) : Color.FromArgb(255, 220, 220);
        _connectButton.Enabled = !connected;
        _disconnectButton.Enabled = connected;
        _sendButton.Enabled = connected;
        _privateButton.Enabled = connected;
    }
}
