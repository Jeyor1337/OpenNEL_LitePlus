using System.Text.Json;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.type;

namespace OpenNEL_Lite.Message.Config;

internal class SetAdvancedConfigMessage : IWsMessage
{
    public string Type => "set_advanced_config";

    public Task<object?> ProcessAsync(JsonElement root)
    {
        var config = AdvancedConfig.Instance;

        if (root.TryGetProperty("lingQingGeApiKey", out var lqg))
            config.LingQingGeApiKey = lqg.ValueKind == JsonValueKind.Null ? null : lqg.GetString();
        if (root.TryGetProperty("crcSaltApiKey", out var crc))
            config.CrcSaltApiKey = crc.ValueKind == JsonValueKind.Null ? null : crc.GetString();

        config.Save();

        return Task.FromResult<object?>(new
        {
            type = "advanced_config",
            lingQingGeApiKey = config.LingQingGeApiKey,
            crcSaltApiKey = config.CrcSaltApiKey
        });
    }
}
