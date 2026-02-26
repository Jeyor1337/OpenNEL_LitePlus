using System.Text.Json;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.type;

namespace OpenNEL_Lite.Message.Config;

internal class SetProxyConfigMessage : IWsMessage
{
    public string Type => "set_proxy_config";

    public Task<object?> ProcessAsync(JsonElement root)
    {
        var config = ProxyConfig.Load();

        if (root.TryGetProperty("enabled", out var e))
            config.Enabled = e.GetBoolean();
        if (root.TryGetProperty("address", out var a))
            config.Address = a.GetString() ?? config.Address;
        if (root.TryGetProperty("port", out var p))
            config.Port = p.GetInt32();
        if (root.TryGetProperty("username", out var u))
            config.Username = u.ValueKind == JsonValueKind.Null ? null : u.GetString();
        if (root.TryGetProperty("password", out var pw))
            config.Password = pw.ValueKind == JsonValueKind.Null ? null : pw.GetString();

        config.Save();

        return Task.FromResult<object?>(new
        {
            type = "proxy_config",
            enabled = config.Enabled,
            address = config.Address,
            port = config.Port,
            username = config.Username,
            password = config.Password
        });
    }
}
