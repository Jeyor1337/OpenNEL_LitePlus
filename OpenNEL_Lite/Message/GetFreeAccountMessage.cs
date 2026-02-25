using OpenNEL_Lite.Network;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net;
using Serilog;

namespace OpenNEL_Lite.Message.Login;

internal class GetFreeAccountMessage : IWsMessage
{
    public string Type => "get_free_account";
    public async Task<object?> ProcessAsync(JsonElement root)
    {
        var source = TryGetString(root, "source") ?? "register";
        return source == "random"
            ? await FetchRandomAccountAsync(root)
            : await RegisterAccountAsync(root);
    }

    private async Task<object?> RegisterAccountAsync(JsonElement root)
    {
        Log.Information("正在获取4399小号...");
        var status = new { type = "get_free_account_status", status = "processing", message = "获取小号中, 这可能需要点时间..." };
        HttpClient? client = null;
        object? resultPayload = null;
        try
        {
            var apiBaseEnv = Environment.GetEnvironmentVariable("SAMSE_API_BASE");
            var apiBaseReq = TryGetString(root, "apiBase");
            var apiBase = string.IsNullOrWhiteSpace(apiBaseEnv) ? (string.IsNullOrWhiteSpace(apiBaseReq) ? "http://4399.11pw.pw" : apiBaseReq) : apiBaseEnv;
            var timeoutSec = TryGetInt(root, "timeout", 30);
            var userAgent = TryGetString(root, "userAgent") ?? "Samse-4399-Client/1.0";
            var maxRetries = TryGetInt(root, "maxRetries", 3);
            var allowInsecure = TryGetBool(root, "ignoreSslErrors") || string.Equals(Environment.GetEnvironmentVariable("SAMSE_IGNORE_SSL"), "1", StringComparison.OrdinalIgnoreCase);
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.All;
            if (allowInsecure)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeoutSec) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            var url = apiBase.TrimEnd('/') + "/reg4399";
            var payload = new System.Collections.Generic.Dictionary<string, object?>();
            AddIfPresent(payload, root, "username");
            AddIfPresent(payload, root, "password");
            AddIfPresent(payload, root, "idcard");
            AddIfPresent(payload, root, "realname");
            AddIfPresent(payload, root, "captchaId");
            AddIfPresent(payload, root, "captcha");
            HttpResponseMessage? resp = null;
            for (var attempt = 0; attempt < Math.Max(1, maxRetries); attempt++)
            {
                try
                {
                    var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                    resp = await client.PostAsync(url, content);
                    break;
                }
                catch when (attempt < Math.Max(1, maxRetries) - 1)
                {
                    await Task.Delay(1000);
                }
            }
            if (resp == null)
            {
                resultPayload = new { type = "get_free_account_result", success = false, message = "网络错误" };
            }
            else
            {
                var body = await resp.Content.ReadAsStringAsync();
                JsonElement d;
                try
                {
                    d = JsonDocument.Parse(body).RootElement;
                }
                catch (Exception)
                {
                    resultPayload = new { type = "get_free_account_result", success = false, message = "响应解析失败" };
                    goto End;
                }
                var success = d.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True;
                if (success)
                {
                    var username = TryGetString(d, "username") ?? "";
                    var password = TryGetString(d, "password") ?? "";
                    var cookie = TryGetString(d, "cookie");
                    var cookieError = TryGetString(d, "cookieError");
                    Log.Information("获取成功: {Username} {Password}", username, password);
                    resultPayload = new
                    {
                        type = "get_free_account_result",
                        success = true,
                        username = username,
                        password = password,
                        cookie = cookie,
                        cookieError = cookieError,
                        message = "获取成功！"
                    };
                }
                else
                {
                    var requiresCaptcha = d.TryGetProperty("requiresCaptcha", out var rc) && rc.ValueKind == JsonValueKind.True;
                    if (requiresCaptcha)
                    {
                        resultPayload = new
                        {
                            type = "get_free_account_requires_captcha",
                            requiresCaptcha = true,
                            captchaId = TryGetString(d, "captchaId"),
                            captchaImageUrl = TryGetString(d, "captchaImageUrl"),
                            username = TryGetString(d, "username"),
                            password = TryGetString(d, "password"),
                            idcard = TryGetString(d, "idcard"),
                            realname = TryGetString(d, "realname")
                        };
                    }
                    else
                    {
                        resultPayload = new { type = "get_free_account_result", success = false, message = body };
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "错误: {Message}", e.Message);
            resultPayload = new { type = "get_free_account_result", success = false, message = "错误: " + e.Message };
        }
        finally
        {
            client?.Dispose();
        }
    End:
        return new object[] { status, resultPayload ?? new { type = "get_free_account_result", success = false, message = "未知错误" } };
    }

    private async Task<object?> FetchRandomAccountAsync(JsonElement root)
    {
        Log.Information("正在随机获取4399小号...");
        var status = new { type = "get_free_account_status", status = "processing", message = "正在从 API 获取随机小号..." };
        object? resultPayload = null;
        try
        {
            var apiKey = TryGetString(root, "apiKey")
                ?? Environment.GetEnvironmentVariable("NEL_API_KEY")
                ?? "2175a76e-8c58-4532-8ac0-9ea3a8068a6a";
            var apiUrl = TryGetString(root, "apiUrl")
                ?? Environment.GetEnvironmentVariable("NEL_API_URL")
                ?? "https://4399.sbcnm.tech/api/uf/get";

            using var handler = new HttpClientHandler();
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.Add("X-Ciallo", apiKey);
            var resp = await http.GetAsync(apiUrl);
            var body = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var d = doc.RootElement;

            if (resp.IsSuccessStatusCode
                && d.TryGetProperty("code", out var codeProp) && codeProp.GetInt32() == 0
                && d.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.String)
            {
                var data = dataProp.GetString()!;
                var parts = data.Split("----", 2);
                if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
                {
                    var username = parts[0];
                    var password = parts[1];
                    Log.Information("随机获取成功: {Username}", username);
                    resultPayload = new
                    {
                        type = "get_free_account_result",
                        success = true,
                        username,
                        password,
                        message = "获取成功！"
                    };
                }
                else
                {
                    resultPayload = new { type = "get_free_account_result", success = false, message = "数据格式错误: " + data };
                }
            }
            else
            {
                resultPayload = new { type = "get_free_account_result", success = false, message = "获取小号失败: " + body };
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "随机获取小号错误: {Message}", e.Message);
            resultPayload = new { type = "get_free_account_result", success = false, message = "请求失败: " + e.Message };
        }
        return new object[] { status, resultPayload ?? new { type = "get_free_account_result", success = false, message = "未知错误" } };
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.String) return v.GetString();
            if (v.ValueKind == JsonValueKind.Number) return v.ToString();
            if (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) return v.ToString();
        }
        return null;
    }

    private static int TryGetInt(JsonElement root, string name, int def)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number)
            {
                if (v.TryGetInt32(out var n)) return n;
            }
            if (v.ValueKind == JsonValueKind.String)
            {
                if (int.TryParse(v.GetString(), out var n)) return n;
            }
        }
        return def;
    }

    private static void AddIfPresent(System.Collections.Generic.Dictionary<string, object?> dict, JsonElement root, string name)
    {
        var val = TryGetString(root, name);
        if (!string.IsNullOrEmpty(val)) dict[name] = val;
    }

    private static bool TryGetBool(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.True) return true;
            if (v.ValueKind == JsonValueKind.False) return false;
            if (v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        return false;
    }
}
