using System.Text.Json;
using Codexus.Cipher.Entities;
using Codexus.Cipher.Entities.WPFLauncher.NetGame;
using Codexus.OpenSDK;
using Codexus.OpenSDK.Entities.X19;
using OpenNEL_Lite.Manager;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.Utils;
using Serilog;

namespace OpenNEL_Lite.Message.Game;

internal class SearchServerMessage : IWsMessage
{
    public string Type => "search_server";

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var keyword = root.TryGetProperty("keyword", out var kw) ? kw.GetString() : null;
        var last = UserManager.Instance.GetLastAvailableUser();
        if (last == null) return new { type = "notlogin" };
        if (string.IsNullOrWhiteSpace(keyword))
            return new { type = "search_server_error", message = "关键词不能为空" };

        try
        {
            var auth = new X19AuthenticationOtp { EntityId = last.UserId, Token = last.AccessToken };
            var servers = await auth.Api<EntityNetGameKeyword, Entities<EntityNetGameItem>>(
                "/item/query/search-by-keyword",
                new EntityNetGameKeyword { Keyword = keyword });

            var items = servers.Data.Select(s => new { name = s.Name, entityId = s.EntityId }).ToArray();
            return new { type = "search_server_result", items, keyword };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "搜索服务器失败: keyword={Keyword}", keyword);
            return new { type = "search_server_error", message = "搜索失败" };
        }
    }
}
