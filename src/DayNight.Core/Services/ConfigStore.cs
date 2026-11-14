using System.Text.Json;
using System.Text.Json.Serialization;
using DayNight.Core.Models;

namespace DayNight.Core.Services;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public string ConfigPath { get; }

    public ConfigStore(string? rootDirectory = null)
    {
        var root = rootDirectory ?? AppContext.BaseDirectory;
        ConfigPath = Path.Combine(root, "config.json");
    }

    public AppConfig LoadOrCreate()
    {
        if (!File.Exists(ConfigPath))
        {
            var created = new AppConfig();
            Save(created);
            return created;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            Normalize(cfg);
            return cfg;
        }
        catch
        {
            var fallback = new AppConfig();
            Save(fallback);
            return fallback;
        }
    }

    public void Save(AppConfig config)
    {
        Normalize(config);
        var dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    private static void Normalize(AppConfig config)
    {
        config.Version = AppConfig.CurrentVersion;
        config.Schedule.LightAt = NormalizeTime(config.Schedule.LightAt, "07:00");
        config.Schedule.DarkAt = NormalizeTime(config.Schedule.DarkAt, "19:00");
        config.Display.Contrast = Clamp(config.Display.Contrast, 0.6, 1.6);
        config.Display.Brightness = Clamp(config.Display.Brightness, -0.25, 0.25);
        config.Display.Gamma = Clamp(config.Display.Gamma, 0.6, 1.8);
        if (config.General.CheckIntervalSeconds < 5)
        {
            config.General.CheckIntervalSeconds = 5;
        }

        var mode = (config.Display.CabcMode ?? "system").Trim().ToLowerInvariant();
        config.Display.CabcMode = mode is "off" or "always" or "battery" or "system"
            ? mode
            : "system";
    }

    private static string NormalizeTime(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (TimeSpan.TryParse(value, out var ts))
        {
            return $"{ts.Hours:D2}:{ts.Minutes:D2}";
        }

        return fallback;
    }

    private static double Clamp(double v, double min, double max)
        => Math.Min(max, Math.Max(min, v));
}
