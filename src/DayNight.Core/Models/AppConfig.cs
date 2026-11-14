using System.Text.Json.Serialization;

namespace DayNight.Core.Models;

public sealed class AppConfig
{
    public const int CurrentVersion = 1;

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "auto";

    [JsonPropertyName("schedule")]
    public ScheduleConfig Schedule { get; set; } = new();

    [JsonPropertyName("theme")]
    public ThemeConfig Theme { get; set; } = new();

    [JsonPropertyName("display")]
    public DisplayConfig Display { get; set; } = new();

    [JsonPropertyName("general")]
    public GeneralConfig General { get; set; } = new();
}

public sealed class ScheduleConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>HH:mm — switch to light theme.</summary>
    [JsonPropertyName("light_at")]
    public string LightAt { get; set; } = "07:00";

    /// <summary>HH:mm — switch to dark theme.</summary>
    [JsonPropertyName("dark_at")]
    public string DarkAt { get; set; } = "19:00";
}

public sealed class ThemeConfig
{
    [JsonPropertyName("change_apps")]
    public bool ChangeApps { get; set; } = true;

    [JsonPropertyName("change_system")]
    public bool ChangeSystem { get; set; } = true;

    [JsonPropertyName("apply_accent")]
    public bool ApplyAccent { get; set; }

    /// <summary>ARGB hex, e.g. #FF0078D4</summary>
    [JsonPropertyName("accent_color")]
    public string AccentColor { get; set; } = "#FF0078D4";

    [JsonPropertyName("show_accent_on_start_and_taskbar")]
    public bool ShowAccentOnStartAndTaskbar { get; set; }
}

public sealed class DisplayConfig
{
    [JsonPropertyName("gamma_enabled")]
    public bool GammaEnabled { get; set; }

    /// <summary>1.0 = neutral. Typical comfort range 0.7–1.4.</summary>
    [JsonPropertyName("contrast")]
    public double Contrast { get; set; } = 1.08;

    /// <summary>-0.2..0.2 relative lift.</summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; set; } = 0.0;

    /// <summary>Gamma exponent, 1.0 = linear/neutral.</summary>
    [JsonPropertyName("gamma")]
    public double Gamma { get; set; } = 1.0;

    /// <summary>off | always | battery | system</summary>
    [JsonPropertyName("cabc_mode")]
    public string CabcMode { get; set; } = "system";

    [JsonPropertyName("manage_cabc")]
    public bool ManageCabc { get; set; }
}

public sealed class GeneralConfig
{
    [JsonPropertyName("start_with_windows")]
    public bool StartWithWindows { get; set; }

    [JsonPropertyName("start_minimized")]
    public bool StartMinimized { get; set; } = false;

    [JsonPropertyName("check_interval_seconds")]
    public int CheckIntervalSeconds { get; set; } = 30;
}
