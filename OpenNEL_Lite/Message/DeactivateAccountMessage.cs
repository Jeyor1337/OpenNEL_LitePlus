using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;
using System.Text.Json;

namespace OpenNEL_Lite.Message.Login;

internal class DeactivateAccountMessage : IWsMessage
{
    public string Type => "deactivate_account";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var id = root.TryGetProperty("entityId", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
        {
            return new { type = "deactivate_account_error", message = "entityId为空" };
        }

        UserManager.Instance.RemoveAvailableUser(id);
        var users = UserManager.Instance.GetUsersNoDetails();
        var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
        return new { type = "accounts", items };
    }
}
