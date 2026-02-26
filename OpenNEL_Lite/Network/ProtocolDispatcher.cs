using System.Collections;
using System.Text.Json;
using OpenNEL_Lite.Message;
using OpenNEL_Lite.type;
using Serilog;

namespace OpenNEL_Lite.Network;

internal static class ProtocolDispatcher
{
    public static async Task<List<object>> DispatchAsync(string json)
    {
        var results = new List<object>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;

        if (string.IsNullOrWhiteSpace(type))
            return results;

        var handler = MessageFactory.Get(type);
        if (handler == null)
            return results;

        object? payload;
        try
        {
            payload = await handler.ProcessAsync(root);
        }
        catch (Exception ex)
        {
            payload = new { type = type + "_error", message = ex.Message };
        }

        if (payload != null)
            Expand(payload, results);

        return results;
    }

    static void Expand(object payload, List<object> results)
    {
        if (payload is string)
        {
            results.Add(payload);
            return;
        }

        if (payload is IEnumerable seq)
        {
            foreach (var item in seq)
            {
                if (item != null)
                    results.Add(item);
            }
            return;
        }

        results.Add(payload);
    }
}
