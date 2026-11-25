namespace ChatCommon;

public static class Protocol
{
    public const int DefaultPort = 5055;

    public static string Join(string username) => $"JOIN|{username}";

    public static string Leave() => "LEAVE";

    public static string Broadcast(string text) => $"MSG|{text}";

    public static string Private(string target, string text) => $"PRIV|{target}|{text}";
}
