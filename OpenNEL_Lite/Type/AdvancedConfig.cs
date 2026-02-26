using System.Text.Json;

namespace OpenNEL_Lite.type;

internal class AdvancedConfig
{
    public string? LingQingGeApiKey { get; set; }
    public string? CrcSaltApiKey { get; set; }

    private const string FilePath = "advanced.json";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    private static AdvancedConfig? _instance;

    public static AdvancedConfig Instance
    {
        get
        {
            _instance ??= Load();
            return _instance;
        }
    }

    public static AdvancedConfig Load()
    {
        if (!File.Exists(FilePath))
            return new AdvancedConfig();

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AdvancedConfig>(json) ?? new AdvancedConfig();
        }
        catch
        {
            return new AdvancedConfig();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
        _instance = this;
    }
}
