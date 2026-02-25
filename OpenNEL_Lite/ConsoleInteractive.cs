using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OpenNEL_Lite.type;

namespace OpenNEL_Lite;

internal class ConsoleInteractive
{
    private readonly int _port;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private byte[] _rest = [];
    private ProxyConfig _proxyConfig = ProxyConfig.Load();

    public ConsoleInteractive(int port)
    {
        _port = port;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", _port);
                _stream = _client.GetStream();

                var line = await ReadLineAsync(5);
                if (line != null) PrintResponse(line);

                await SendJsonAsync(new { ping = "ok" });
                line = await ReadLineAsync(5);
                if (line != null) PrintResponse(line);

                await MenuLoopAsync();
                return;
            }
            catch (Exception ex)
            {
                PrintColored($"连接失败: {ex.Message}", ConsoleColor.Red);
            }
            finally
            {
                try { _client?.Close(); } catch { }
            }

            Console.WriteLine();
            if (!ConfirmRetry("连接已断开，是否重新连接?")) return;
        }
    }

    async Task MenuLoopAsync()
    {
        string[] items =
        [
            "4399 登录",
            "查看账号列表",
            "加入游戏",
            "关闭游戏",
            "发送自定义消息",
            "代理设置",
            "关于",
            "退出"
        ];

        while (true)
        {
            var choice = SelectMenu("OpenNEL Lite Plus", items);
            if (choice == 7) return;

            Console.Clear();
            try
            {
                switch (choice)
                {
                    case 0: await Login4399Async(); break;
                    case 1: await ListAccountsAsync(); break;
                    case 2: await JoinGameAsync(); break;
                    case 3: await ShutdownGameAsync(); break;
                    case 4: await SendCustomAsync(); break;
                    case 5: ProxySettingsMenu(); break;
                    case 6: ShowAbout(); break;
                }
            }
            catch (Exception ex)
            {
                PrintColored($"  操作失败: {ex.Message}", ConsoleColor.Red);
            }

            Console.WriteLine();
            PrintColored("按任意键返回主菜单...", ConsoleColor.DarkYellow);
            Console.ReadKey(true);
        }
    }

    static int SelectMenu(string title, string[] items, bool showEscHint = false)
    {
        var index = 0;
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("══════════════════════════════════════");
            Console.WriteLine($"  {title}");
            Console.WriteLine("══════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine();

            for (var i = 0; i < items.Length; i++)
            {
                if (i == index)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.Write($"  > {items[i]}  ");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"    {items[i]}");
                }
            }

            Console.WriteLine();
            var hint = showEscHint ? "  ↑↓ 选择  Enter 确认  Esc 返回" : "  ↑↓ 选择  Enter 确认";
            PrintColored(hint, ConsoleColor.DarkGray);

            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    index = (index - 1 + items.Length) % items.Length;
                    break;
                case ConsoleKey.DownArrow:
                    index = (index + 1) % items.Length;
                    break;
                case ConsoleKey.Enter:
                    return index;
                case ConsoleKey.Escape when showEscHint:
                    return -1;
            }
        }
    }

    async Task Login4399Async()
    {
        var items = new[] { "手动输入账号密码", "随机获取小号", "返回" };
        var choice = SelectMenu("4399 登录", items, true);
        if (choice == 2 || choice == -1) return;

        Console.Clear();
        if (choice == 0)
            await Login4399ManualAsync();
        else
            await Login4399RandomAsync();
    }

    async Task Login4399ManualAsync()
    {
        while (true)
        {
            Console.Clear();
            PrintColored("── 4399 登录 ──", ConsoleColor.Cyan);
            Console.WriteLine();
            Console.Write("  账号 (输入空行返回): ");
            var account = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(account)) return;

            Console.Write("  密码 (Esc 返回): ");
            var password = ReadPasswordMasked();
            Console.WriteLine();
            if (password == null) return;

            if (string.IsNullOrEmpty(password))
            {
                PrintColored("  密码不能为空，请重新输入", ConsoleColor.Red);
                WaitKey();
                continue;
            }

            PrintColored("  正在登录...", ConsoleColor.Yellow);
            await SendJsonAsync(new { type = "login_4399", account, password });
            await ReadAllResponsesAsync(15);
            return;
        }
    }

    async Task Login4399RandomAsync()
    {
        Console.Clear();
        PrintColored("── 随机获取小号 ──", ConsoleColor.Cyan);
        Console.WriteLine();

        var apiKey = Environment.GetEnvironmentVariable("NEL_API_KEY") ?? "****";
        var apiUrl = Environment.GetEnvironmentVariable("NEL_API_URL") ?? "https://4399.sbcnm.tech/api/uf/get";

        while (true)
        {
            PrintColored("  正在从 API 获取小号...", ConsoleColor.Yellow);

            string? account = null;
            string? password = null;
            var fetchSuccess = false;

            try
            {
                using var handler = new HttpClientHandler();
                using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
                http.DefaultRequestHeaders.Add("X-Ciallo", apiKey);
                var resp = await http.GetAsync(apiUrl);
                var body = await resp.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (resp.IsSuccessStatusCode
                    && root.TryGetProperty("code", out var codeProp) && codeProp.GetInt32() == 0
                    && root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.String)
                {
                    var data = dataProp.GetString()!;
                    var parts = data.Split("----", 2);
                    if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
                    {
                        account = parts[0];
                        password = parts[1];
                        fetchSuccess = true;
                    }
                }

                if (!fetchSuccess)
                    PrintColored($"  获取小号失败: {body}", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                PrintColored($"  请求失败: {ex.Message}", ConsoleColor.Red);
            }

            if (!fetchSuccess)
            {
                var items = new[] { "重试", "返回主菜单" };
                var choice = SelectMenu("获取小号失败", items);
                if (choice == 1) return;
                Console.Clear();
                PrintColored("── 随机获取小号 ──", ConsoleColor.Cyan);
                Console.WriteLine();
                continue;
            }

            PrintColored($"  获取成功: {account}", ConsoleColor.Green);
            PrintColored("  正在登录...", ConsoleColor.Yellow);
            await SendJsonAsync(new { type = "login_4399", account, password });

            var loginSuccess = false;
            var endTime = DateTime.UtcNow.AddSeconds(15);
            var gotAny = false;
            while (DateTime.UtcNow < endTime)
            {
                var left = (int)(endTime - DateTime.UtcNow).TotalSeconds;
                var timeout = gotAny ? Math.Min(left, 2) : left;
                var line = await ReadLineAsync(Math.Max(1, timeout));
                if (line == null) break;
                gotAny = true;
                PrintResponse(line);

                try
                {
                    using var respDoc = JsonDocument.Parse(line);
                    var respRoot = respDoc.RootElement;
                    var respType = respRoot.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                    if (respType == "Success_login")
                        loginSuccess = true;
                }
                catch { }
            }

            if (loginSuccess) return;

            PrintColored("  登录失败，10秒后重新获取小号...", ConsoleColor.Yellow);
            await Task.Delay(TimeSpan.FromSeconds(10));
            Console.Clear();
            PrintColored("── 随机获取小号 ──", ConsoleColor.Cyan);
            Console.WriteLine();
        }
    }

    async Task ListAccountsAsync()
    {
        PrintColored("── 账号列表 ──", ConsoleColor.Cyan);
        await SendJsonAsync(new { type = "list_accounts" });
        await ReadAllResponsesAsync(5);
    }

    static readonly (string Name, string Id)[] BuiltInServers =
    [
        ("布吉岛", "4661334467366178884"),
    ];

    async Task JoinGameAsync()
    {
        Console.Clear();
        PrintColored("── 加入游戏 ──", ConsoleColor.Cyan);
        Console.WriteLine();

        var serverMenuItems = BuiltInServers.Select(s => $"{s.Name} ({s.Id})").ToList();
        serverMenuItems.Add("[手动输入服务器ID]");
        serverMenuItems.Add("返回");

        var serverChoice = SelectMenu("选择服务器", serverMenuItems.ToArray(), true);
        if (serverChoice == -1 || serverChoice == serverMenuItems.Count - 1) return;

        string serverId;
        if (serverChoice < BuiltInServers.Length)
        {
            serverId = BuiltInServers[serverChoice].Id;
        }
        else
        {
            Console.Write("  服务器ID (输入空行返回): ");
            serverId = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(serverId)) return;
        }

        PrintColored("  正在获取角色列表...", ConsoleColor.Yellow);
        await SendJsonAsync(new { type = "open_server", serverId });

        var roles = new List<string>();
        var endTime = DateTime.UtcNow.AddSeconds(15);
        var gotAny = false;
        while (DateTime.UtcNow < endTime)
        {
            var left = (int)(endTime - DateTime.UtcNow).TotalSeconds;
            var timeout = gotAny ? Math.Min(left, 2) : left;
            var line = await ReadLineAsync(Math.Max(1, timeout));
            if (line == null) break;
            gotAny = true;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var type = root.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";

                if (type == "server_roles")
                {
                    if (root.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
                        foreach (var item in arr.EnumerateArray())
                        {
                            var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                            if (!string.IsNullOrEmpty(name)) roles.Add(name);
                        }
                }
                else if (type == "notlogin")
                {
                    PrintColored("  未登录，请先登录账号", ConsoleColor.Red);
                    return;
                }
                else if (type == "server_roles_error")
                {
                    var msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
                    PrintColored($"  获取角色失败: {msg}", ConsoleColor.Red);
                    return;
                }
                else
                {
                    PrintResponse(line);
                }
            }
            catch
            {
                PrintResponse(line);
            }
        }

        if (roles.Count == 0)
        {
            PrintColored("  该服务器没有角色", ConsoleColor.DarkGray);
            Console.WriteLine();

            var createItems = new[] { "创建新角色", "返回" };
            var cc = SelectMenu("加入游戏 - " + serverId, createItems, true);
            if (cc == 0)
                await CreateRoleAndJoinAsync(serverId, roles);
            return;
        }

        while (true)
        {
            var menuItems = new List<string>(roles);
            if (roles.Count < 3)
                menuItems.Add("[创建新角色]");
            menuItems.Add("返回");

            var choice = SelectMenu("加入游戏 - 选择角色", menuItems.ToArray(), true);
            if (choice == -1 || choice == menuItems.Count - 1) return;

            if (roles.Count < 3 && choice == menuItems.Count - 2)
            {
                await CreateRoleAndJoinAsync(serverId, roles);
                if (roles.Count == 0) return;

                menuItems = new List<string>(roles);
                if (roles.Count < 3)
                    menuItems.Add("[创建新角色]");
                menuItems.Add("返回");
                continue;
            }

            var selectedRole = roles[choice];
            Console.Clear();
            var serverName = BuiltInServers.FirstOrDefault(s => s.Id == serverId).Name ?? "";
            if (string.IsNullOrEmpty(serverName))
            {
                Console.Write("  服务器名称 (可选, 回车跳过): ");
                serverName = Console.ReadLine()?.Trim() ?? "";
            }

            PrintColored($"  正在以 [{selectedRole}] 加入游戏...", ConsoleColor.Yellow);
            await SendJsonAsync(new
            {
                type = "join_game", serverId, role = selectedRole, serverName,
                socks5 = new
                {
                    enabled = _proxyConfig.Enabled,
                    address = _proxyConfig.Address,
                    port = _proxyConfig.Port,
                    username = _proxyConfig.Username,
                    password = _proxyConfig.Password
                }
            });
            await ReadAllResponsesAsync(30);
            return;
        }
    }

    async Task CreateRoleAndJoinAsync(string serverId, List<string> roles)
    {
        Console.Clear();
        PrintColored("── 创建新角色 ──", ConsoleColor.Cyan);
        Console.WriteLine();
        Console.Write("  角色名称 (输入空行返回): ");
        var name = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrEmpty(name)) return;

        PrintColored("  正在创建角色...", ConsoleColor.Yellow);
        await SendJsonAsync(new { type = "create_role_named", serverId, name });

        var endTime = DateTime.UtcNow.AddSeconds(15);
        var gotAny = false;
        while (DateTime.UtcNow < endTime)
        {
            var left = (int)(endTime - DateTime.UtcNow).TotalSeconds;
            var timeout = gotAny ? Math.Min(left, 2) : left;
            var line = await ReadLineAsync(Math.Max(1, timeout));
            if (line == null) break;
            gotAny = true;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var type = root.TryGetProperty("type", out var tp) ? tp.GetString() ?? "" : "";

                if (type == "server_roles")
                {
                    roles.Clear();
                    if (root.TryGetProperty("entities", out var ent))
                    {
                        if (ent.TryGetProperty("Data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                            foreach (var item in dataArr.EnumerateArray())
                            {
                                var n = item.TryGetProperty("Name", out var np) ? np.GetString() ?? "" : "";
                                if (!string.IsNullOrEmpty(n)) roles.Add(n);
                            }
                        else if (ent.TryGetProperty("data", out var dataArr2) && dataArr2.ValueKind == JsonValueKind.Array)
                            foreach (var item in dataArr2.EnumerateArray())
                            {
                                var n = item.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                                if (!string.IsNullOrEmpty(n)) roles.Add(n);
                            }
                    }
                    if (root.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        roles.Clear();
                        foreach (var item in arr.EnumerateArray())
                        {
                            var n = item.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                            if (!string.IsNullOrEmpty(n)) roles.Add(n);
                        }
                    }
                    PrintColored($"  角色创建成功: {name}", ConsoleColor.Green);
                    return;
                }
                else if (type == "server_roles_error")
                {
                    var msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
                    PrintColored($"  创建角色失败: {msg}", ConsoleColor.Red);
                    WaitKey();
                    return;
                }
                else
                {
                    PrintResponse(line);
                }
            }
            catch
            {
                PrintResponse(line);
            }
        }
    }

    async Task ShutdownGameAsync()
    {
        while (true)
        {
            Console.Clear();
            PrintColored("── 关闭游戏 ──", ConsoleColor.Cyan);
            Console.WriteLine();
            Console.Write("  游戏标识符 (GUID, 多个用逗号分隔, 输入空行返回): ");
            var input = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(input)) return;

            var identifiers = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (identifiers.Length == 0)
            {
                PrintColored("  输入无效，请重新输入", ConsoleColor.Red);
                WaitKey();
                continue;
            }

            PrintColored("  正在关闭...", ConsoleColor.Yellow);
            await SendJsonAsync(new { type = "shutdown_game", identifiers });
            await ReadAllResponsesAsync(10);
            return;
        }
    }

    async Task SendCustomAsync()
    {
        Console.Clear();
        PrintColored("── 发送自定义消息 ──", ConsoleColor.Cyan);
        Console.WriteLine();
        Console.Write("  JSON (输入空行返回): ");
        var text = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrEmpty(text)) return;

        await SendRawAsync(text);
        await ReadAllResponsesAsync(10);
    }

    void ProxySettingsMenu()
    {
        while (true)
        {
            var status = _proxyConfig.Enabled ? "已开启" : "已关闭";
            var title = $"代理设置 [{status}]";
            var items = new[]
            {
                _proxyConfig.Enabled ? "关闭代理" : "开启代理",
                $"服务器地址: {_proxyConfig.Address}",
                $"端口: {_proxyConfig.Port}",
                $"用户名: {_proxyConfig.Username ?? "(未设置)"}",
                $"密码: {(_proxyConfig.Password != null ? "******" : "(未设置)")}",
                "返回"
            };

            var choice = SelectMenu(title, items, true);
            if (choice == -1 || choice == 5) return;

            Console.Clear();
            switch (choice)
            {
                case 0:
                    _proxyConfig.Enabled = !_proxyConfig.Enabled;
                    _proxyConfig.Save();
                    PrintColored($"  代理已{(_proxyConfig.Enabled ? "开启" : "关闭")}", _proxyConfig.Enabled ? ConsoleColor.Green : ConsoleColor.Yellow);
                    WaitKey();
                    break;
                case 1:
                    Console.Write("  服务器地址 (输入空行保持不变): ");
                    var addr = Console.ReadLine()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(addr))
                    {
                        _proxyConfig.Address = addr;
                        _proxyConfig.Save();
                    }
                    break;
                case 2:
                    Console.Write("  端口 (输入空行保持不变): ");
                    var portStr = Console.ReadLine()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out var port))
                    {
                        _proxyConfig.Port = port;
                        _proxyConfig.Save();
                    }
                    break;
                case 3:
                    Console.Write("  用户名 (输入空行清除): ");
                    var user = Console.ReadLine()?.Trim() ?? "";
                    _proxyConfig.Username = string.IsNullOrEmpty(user) ? null : user;
                    _proxyConfig.Save();
                    break;
                case 4:
                    Console.Write("  密码 (Esc 取消, 回车清除): ");
                    var pwd = ReadPasswordMasked();
                    Console.WriteLine();
                    if (pwd != null)
                    {
                        _proxyConfig.Password = string.IsNullOrEmpty(pwd) ? null : pwd;
                        _proxyConfig.Save();
                    }
                    break;
            }
        }
    }

    static void ShowAbout()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("══════════════════════════════════════");
        Console.WriteLine("  关于 OpenNEL Lite Plus");
        Console.WriteLine("══════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
        PrintColored("  OpenNEL Lite Plus 基于开源项目 OpenNEL Lite 构建。", ConsoleColor.White);
        Console.WriteLine();
        PrintColored("  原始项目:", ConsoleColor.DarkYellow);
        Console.WriteLine("    名称: OpenNEL Lite");
        Console.WriteLine("    仓库: https://github.com/AimCloudX/OpenNEL_Lite");
        Console.WriteLine("    协议: MIT License");
        Console.WriteLine();
        PrintColored("  致谢:", ConsoleColor.DarkYellow);
        Console.WriteLine("    感谢 OpenNEL Lite 项目及其贡献者提供的开源基础代码。");
        Console.WriteLine("    本项目在其基础上进行了二次开发与功能扩展。");
        Console.WriteLine();
        PrintColored("  依赖项目:", ConsoleColor.DarkYellow);
        Console.WriteLine("    Codexus SDK - 核心 SDK 支持");
    }

    async Task SendJsonAsync(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        PrintColored($"  [发送] {json}", ConsoleColor.DarkGreen);
        await _stream!.WriteAsync(Encoding.UTF8.GetBytes(json + "\n"));
    }

    async Task SendRawAsync(string text)
    {
        PrintColored($"  [发送] {text}", ConsoleColor.DarkGreen);
        await _stream!.WriteAsync(Encoding.UTF8.GetBytes(text + "\n"));
    }

    async Task<string?> ReadLineAsync(int timeoutSec)
    {
        var buf = new List<byte>(_rest);
        _rest = [];
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSec);
        var tmp = new byte[4096];

        while (DateTime.UtcNow < deadline)
        {
            var nl = buf.IndexOf((byte)'\n');
            if (nl >= 0)
            {
                var line = Encoding.UTF8.GetString(buf.ToArray(), 0, nl).TrimEnd('\r');
                _rest = buf.Skip(nl + 1).ToArray();
                return line;
            }

            try
            {
                if (_stream!.DataAvailable)
                {
                    var n = await _stream.ReadAsync(tmp, 0, tmp.Length);
                    if (n <= 0) return null;
                    buf.AddRange(new ArraySegment<byte>(tmp, 0, n));
                }
                else
                {
                    await Task.Delay(50);
                }
            }
            catch
            {
                return null;
            }
        }

        _rest = buf.ToArray();
        return null;
    }

    async Task ReadAllResponsesAsync(int maxWaitSec)
    {
        var endTime = DateTime.UtcNow.AddSeconds(maxWaitSec);
        var gotAny = false;
        while (DateTime.UtcNow < endTime)
        {
            var left = (int)(endTime - DateTime.UtcNow).TotalSeconds;
            var timeout = gotAny ? Math.Min(left, 2) : left;
            var line = await ReadLineAsync(Math.Max(1, timeout));
            if (line == null) break;
            gotAny = true;
            PrintResponse(line);
        }
    }

    void PrintResponse(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

            Console.WriteLine();
            switch (type)
            {
                case "Success_login":
                    PrintColored("  ✓ 登录成功", ConsoleColor.Green);
                    PrintField("账号ID", root, "entityId");
                    PrintField("渠道", root, "channel");
                    break;

                case "login_4399_error":
                    PrintColored("  ✗ 登录失败", ConsoleColor.Red);
                    PrintField("原因", root, "message");
                    break;

                case "accounts":
                    PrintColored("  ── 账号列表 ──", ConsoleColor.Cyan);
                    if (root.TryGetProperty("items", out var accItems) && accItems.ValueKind == JsonValueKind.Array)
                    {
                        var idx = 1;
                        foreach (var item in accItems.EnumerateArray())
                        {
                            var eid = item.TryGetProperty("entityId", out var e) ? e.ToString() : "?";
                            var ch = item.TryGetProperty("channel", out var c) ? c.ToString() : "?";
                            var st = item.TryGetProperty("status", out var s) ? s.GetString() ?? "?" : "?";
                            var stColor = st == "online" ? ConsoleColor.Green : ConsoleColor.DarkGray;
                            Console.Write($"    {idx}. ");
                            Console.Write($"[{eid}] ");
                            PrintColored($"      渠道: {ch}  状态: {st}", stColor);
                            idx++;
                        }
                        if (idx == 1) PrintColored("    (无账号)", ConsoleColor.DarkGray);
                    }
                    break;

                case "server_roles":
                    PrintColored("  ── 角色列表 ──", ConsoleColor.Cyan);
                    PrintField("服务器", root, "serverId");
                    if (root.TryGetProperty("items", out var roleItems) && roleItems.ValueKind == JsonValueKind.Array)
                    {
                        var idx = 1;
                        foreach (var item in roleItems.EnumerateArray())
                        {
                            var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
                            Console.WriteLine($"    {idx}. {name}");
                            idx++;
                        }
                        if (idx == 1) PrintColored("    (无角色)", ConsoleColor.DarkGray);
                    }
                    break;

                case "server_roles_error":
                    PrintColored("  ✗ 获取角色失败", ConsoleColor.Red);
                    PrintField("原因", root, "message");
                    break;

                case "notlogin":
                    PrintColored("  ✗ 未登录，请先登录账号", ConsoleColor.Red);
                    break;

                case "channels_updated":
                    PrintColored("  ✓ 游戏状态已更新", ConsoleColor.Green);
                    if (root.TryGetProperty("address", out _)) PrintField("地址", root, "address");
                    if (root.TryGetProperty("guid", out _)) PrintField("标识符", root, "guid");
                    break;

                case "start_error":
                    PrintColored("  ✗ 启动失败", ConsoleColor.Red);
                    PrintField("原因", root, "message");
                    break;

                case "shutdown_ack":
                    PrintColored("  ✓ 已关闭", ConsoleColor.Green);
                    if (root.TryGetProperty("identifiers", out var ids) && ids.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var id in ids.EnumerateArray())
                            Console.WriteLine($"    - {id.GetString()}");
                    }
                    break;

                case "get_free_account_status":
                    PrintColored($"  ⟳ {(root.TryGetProperty("message", out var sm) ? sm.GetString() : "处理中...")}", ConsoleColor.Yellow);
                    break;

                case "get_free_account_result":
                    var success = root.TryGetProperty("success", out var sp) && sp.ValueKind == JsonValueKind.True;
                    if (success)
                    {
                        PrintColored("  ✓ 获取小号成功", ConsoleColor.Green);
                        PrintField("账号", root, "username");
                        PrintField("密码", root, "password");
                        if (root.TryGetProperty("cookieError", out var ce) && ce.ValueKind == JsonValueKind.String)
                            PrintField("Cookie错误", root, "cookieError");
                    }
                    else
                    {
                        PrintColored("  ✗ 获取小号失败", ConsoleColor.Red);
                        PrintField("原因", root, "message");
                    }
                    break;

                case "get_free_account_requires_captcha":
                    PrintColored("  ! 需要验证码", ConsoleColor.Yellow);
                    PrintField("验证码ID", root, "captchaId");
                    PrintField("验证码图片", root, "captchaImageUrl");
                    break;

                default:
                    PrintColored($"  [响应] type={type}", ConsoleColor.Green);
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name == "type") continue;
                        var val = prop.Value.ValueKind == JsonValueKind.String
                            ? prop.Value.GetString() ?? ""
                            : prop.Value.GetRawText();
                        Console.WriteLine($"    {prop.Name}: {val}");
                    }
                    break;
            }
        }
        catch
        {
            PrintColored($"  [收到] {line}", ConsoleColor.Green);
        }
    }

    static void PrintField(string label, JsonElement root, string propName)
    {
        if (root.TryGetProperty(propName, out var val))
        {
            var text = val.ValueKind == JsonValueKind.String ? val.GetString() ?? "" : val.GetRawText();
            Console.WriteLine($"    {label}: {text}");
        }
    }

    static string? ReadPasswordMasked()
    {
        var sb = new StringBuilder();
        while (true)
        {
            var ki = Console.ReadKey(true);
            if (ki.Key == ConsoleKey.Enter) break;
            if (ki.Key == ConsoleKey.Escape) return null;
            if (ki.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                    Console.Write("\b \b");
                }
                continue;
            }
            if (ki.KeyChar != '\0')
            {
                sb.Append(ki.KeyChar);
                Console.Write('*');
            }
        }
        return sb.ToString();
    }

    static void PrintColored(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }

    static bool ConfirmRetry(string message)
    {
        var items = new[] { "重试", "退出" };
        var choice = SelectMenu(message, items);
        return choice == 0;
    }

    static void WaitKey()
    {
        PrintColored("  按任意键继续...", ConsoleColor.DarkYellow);
        Console.ReadKey(true);
    }
}
