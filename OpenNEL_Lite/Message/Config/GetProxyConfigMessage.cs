using System.Text.Json;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.type;

namespace OpenNEL_Lite.Message.Config;

internal class GetProxyConfigMessage : IWsMessage
{
    public string Type => "get_proxy_config";

    public Task<object?> ProcessAsync(JsonElement root)
    {
        var config = ProxyConfig.Load();
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
