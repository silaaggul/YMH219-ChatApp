namespace ChatCommon;

public sealed class ChatMessage
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Sender { get; init; } = "Sistem";
    public string Text { get; init; } = string.Empty;
    public bool IsPrivate { get; init; }

    public string ToWire()
    {
        string flag = IsPrivate ? "1" : "0";
        return $"{Timestamp:O}|{flag}|{Escape(Sender)}|{Escape(Text)}";
    }

    public static bool TryParse(string line, out ChatMessage message)
    {
        message = new ChatMessage();
        string[] parts = line.Split('|', 4);
        if (parts.Length < 4)
        {
            return false;
        }

        if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime ts))
        {
            ts = DateTime.Now;
        }

        message = new ChatMessage
        {
            Timestamp = ts,
            IsPrivate = parts[1] == "1",
            Sender = Unescape(parts[2]),
            Text = Unescape(parts[3])
        };
        return true;
    }

    public override string ToString()
    {
        string kind = IsPrivate ? "özel" : "oda";
        return $"[{Timestamp:HH:mm:ss}] ({kind}) {Sender}: {Text}";
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("|", "\\p").Replace("\n", "\\n");

    private static string Unescape(string value) => value.Replace("\\n", "\n").Replace("\\p", "|").Replace("\\\\", "\\");
}
