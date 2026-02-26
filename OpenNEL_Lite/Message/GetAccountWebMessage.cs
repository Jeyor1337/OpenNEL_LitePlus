using System.Text.Json;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;

namespace OpenNEL_Lite.Message.Connected;

public class GetAccountWebMessage : IWsMessage
{
    public string Type => "get_account";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        return new { type = "accounts", items };
    }
}
