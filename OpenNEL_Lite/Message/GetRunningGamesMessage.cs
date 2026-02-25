using System.Text.Json;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;

namespace OpenNEL_Lite.Message.Game;

internal class GetRunningGamesMessage : IWsMessage
{
    public string Type => "get_running_games";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var items = GameManager.Instance.GetQueryInterceptors();
        return new { type = "running_games", items };
    }
}
