using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using OpenNEL_Lite.Network;
using OpenNEL_Lite.type;
using Serilog;

namespace OpenNEL_Lite;

internal class WebServer
{
    private readonly int _port;
    private WebApplication? _app;

    public WebServer(int port)
    {
        _port = port;
    }

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(o =>
        {
            o.ListenLocalhost(_port);
        });
        builder.Logging.ClearProviders();

        _app = builder.Build();

        _app.UseWebSockets();

        _app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

        _app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocketAsync(ws);
        });

        var wwwroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        if (Directory.Exists(wwwroot))
        {
            var fileProvider = new PhysicalFileProvider(wwwroot);
            _app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
            _app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
            _app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
        }

        await _app.StartAsync();
        Log.Information("-> 访问: http://127.0.0.1:{Port} 使用 Web UI", _port);
    }

    static async Task HandleWebSocketAsync(WebSocket ws)
    {
        var buffer = new byte[8192];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (AppState.Debug)
                    Log.Information("WS Recv: {Text}", json);

                List<object> responses;
                try
                {
                    responses = await ProtocolDispatcher.DispatchAsync(json);
                }
                catch (Exception ex)
                {
                    responses = [new { type = "error", message = ex.Message }];
                }

                foreach (var resp in responses)
                {
                    var respJson = JsonSerializer.Serialize(resp);
                    var bytes = Encoding.UTF8.GetBytes(respJson);
                    await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
        catch (WebSocketException) { }
        finally
        {
            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                }
                catch { }
            }
        }
    }

    public async Task StopAsync()
    {
        if (_app != null)
            await _app.StopAsync();
    }
}
