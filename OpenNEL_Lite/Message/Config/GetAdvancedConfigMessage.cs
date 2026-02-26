using System.Text.Json;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.type;

namespace OpenNEL_Lite.Message.Config;

internal class GetAdvancedConfigMessage : IWsMessage
{
    public string Type => "get_advanced_config";

    public Task<object?> ProcessAsync(JsonElement root)
    {
        var config = AdvancedConfig.Instance;
        return Task.FromResult<object?>(new
        {
            type = "advanced_config",
            lingQingGeApiKey = config.LingQingGeApiKey,
            crcSaltApiKey = config.CrcSaltApiKey
        });
    }
}
