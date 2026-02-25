using System.Text.Json;

namespace OpenNEL_Lite.type;

internal class ProxyConfig
{
    public bool Enabled { get; set; }
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 1080;
    public string? Username { get; set; }
    public string? Password { get; set; }

    private const string FilePath = "proxy.json";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static ProxyConfig Load()
    {
        if (!File.Exists(FilePath))
            return new ProxyConfig();

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ProxyConfig>(json) ?? new ProxyConfig();
        }
        catch
        {
            return new ProxyConfig();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
