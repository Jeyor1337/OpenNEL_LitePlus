using System;
using System.Diagnostics;
using System.Text.Json;
using Codexus.Cipher.Entities.WPFLauncher;
using OpenNEL_Lite.Entities.Web.NEL;
using OpenNEL_Lite.Manager;
using Serilog;
using OpenNEL_Lite.type;
using OpenNEL_Lite.Network;

namespace OpenNEL_Lite.Message.Login;

internal class Login4399Message : IWsMessage
{
    public string Type => "login_4399";

    private static readonly string PeSAuthPath = Path.Combine(AppContext.BaseDirectory, "resources", "PeSAuth-x86", "PeSAuth-x86.exe");

    public async Task<object?> ProcessAsync(JsonElement root)
    {
        if (AppState.Debug) Log.Information("WS Recv: {Payload}", root.GetRawText());
        var account = root.TryGetProperty("account", out var acc) ? acc.GetString() : string.Empty;
        var password = root.TryGetProperty("password", out var pwd) ? pwd.GetString() : string.Empty;
        try
        {
            AppState.Services!.X19.InitializeDeviceAsync().GetAwaiter().GetResult();

            var cookieJson = await RunPeSAuthAsync(account ?? string.Empty, password ?? string.Empty);

            if (AppState.Debug) Log.Information("PeSAuth cookieJson length: {Length}", cookieJson?.Length ?? 0);
            if (string.IsNullOrWhiteSpace(cookieJson))
            {
                var err = new { type = "login_4399_error", message = "cookie empty" };
                if (AppState.Debug) Log.Information("WS SendText: {Message}", JsonSerializer.Serialize(err));
                return err;
            }
            EntityX19CookieRequest cookieReq;
            try
            {
                cookieReq = JsonSerializer.Deserialize<EntityX19CookieRequest>(cookieJson) ?? new EntityX19CookieRequest { Json = cookieJson };
            }
            catch (Exception de)
            {
                if (AppState.Debug) Log.Error(de, "Deserialize cookieJson failed: length={Length}", cookieJson?.Length ?? 0);
                cookieReq = new EntityX19CookieRequest { Json = cookieJson };
            }
            var (authOtp, channel) = AppState.X19.LoginWithCookie(cookieReq);
            if (AppState.Debug) Log.Information("X19 LoginWithCookie: {UserId} Channel: {Channel}", authOtp.EntityId, channel);
            UserManager.Instance.AddUserToMaintain(authOtp);
            UserManager.Instance.AddUser(new Entities.Web.EntityUser
            {
                UserId = authOtp.EntityId,
                Authorized = true,
                AutoLogin = false,
                Channel = channel,
                Type = "password",
                Details = JsonSerializer.Serialize(new EntityPasswordRequest { Account = account ?? string.Empty, Password = password ?? string.Empty })
            });
            var list = new System.Collections.ArrayList();
            list.Add(new { type = "Success_login", entityId = authOtp.EntityId, channel });
            var users = UserManager.Instance.GetUsersNoDetails();
            var items = users.Select(u => new { entityId = u.UserId, channel = u.Channel, status = u.Authorized ? "online" : "offline" }).ToArray();
            list.Add(new { type = "accounts", items });
            if (AppState.Debug) Log.Information("WS SendText: {Message}", JsonSerializer.Serialize(list));
            return list;
        }
        catch (System.Exception ex)
        {
            var msg = ex.Message ?? string.Empty;
            if (AppState.Debug) Log.Error(ex, "WS 4399 login exception. account={Account}", account ?? string.Empty);
            var err = new { type = "login_4399_error", message = msg.Length == 0 ? "登录失败" : msg };
            if (AppState.Debug) Log.Information("WS SendText: {Message}", JsonSerializer.Serialize(err));
            return err;
        }
    }

    private static async Task<string> RunPeSAuthAsync(string account, string password)
    {
        var psi = new ProcessStartInfo
        {
            FileName = PeSAuthPath,
            WorkingDirectory = Path.GetDirectoryName(PeSAuthPath)!,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new Exception("PeSAuth启动失败");

        await process.StandardInput.WriteLineAsync(account);
        await process.StandardInput.WriteLineAsync(password);
        await process.StandardInput.WriteLineAsync();
        process.StandardInput.Close();

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!output.Contains("Login succeeded!"))
            throw new Exception("PeSAuth登录失败: " + output.Trim());

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Sauth: "))
                return trimmed.Substring("Sauth: ".Length);
        }

        throw new Exception("PeSAuth未返回Sauth cookie");
    }
}
