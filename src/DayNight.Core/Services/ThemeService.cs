using Microsoft.Win32;
using DayNight.Core.Models;
using DayNight.Core.Native;

namespace DayNight.Core.Services;

public enum ThemeMode
{
    Light,
    Dark
}

public sealed class ThemeService
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private const string DwmKey = @"Software\Microsoft\Windows\DWM";

    private const string ExplorerAccentKey =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";

    public ThemeMode GetCurrentAppTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        var value = key?.GetValue("AppsUseLightTheme");
        return value is int i && i == 1 ? ThemeMode.Light : ThemeMode.Dark;
    }

    public ThemeMode GetDesiredMode(ScheduleConfig schedule, DateTime? now = null)
    {
        var at = now ?? DateTime.Now;
        var light = ParseTime(schedule.LightAt);
        var dark = ParseTime(schedule.DarkAt);
        var tod = at.TimeOfDay;

        if (light == dark)
        {
            return ThemeMode.Light;
        }

        if (light < dark)
        {
            return tod >= light && tod < dark ? ThemeMode.Light : ThemeMode.Dark;
        }

        return tod >= dark && tod < light ? ThemeMode.Dark : ThemeMode.Light;
    }

    /// <summary>
    /// Switch light/dark only. Accent is separate — re-writing it on every toggle
    /// causes extra shell broadcasts and makes switches feel laggy.
    /// </summary>
    public bool ApplyTheme(ThemeMode mode, ThemeConfig theme)
    {
        var lightValue = mode == ThemeMode.Light ? 1 : 0;
        var changed = false;

        using (var key = Registry.CurrentUser.CreateSubKey(PersonalizeKey))
        {
            if (theme.ChangeApps)
            {
                changed |= SetDword(key, "AppsUseLightTheme", (uint)lightValue);
            }

            if (theme.ChangeSystem)
            {
                changed |= SetDword(key, "SystemUsesLightTheme", (uint)lightValue);
            }
        }

        if (changed)
        {
            BroadcastThemeChange();
        }

        return changed;
    }

    /// <summary>
    /// Write Windows accent so Settings / Start / taskbar / immersive UI pick it up.
    /// Win10/11 need Explorer\Accent (+ palette), not only DWM\AccentColor.
    /// </summary>
    public bool ApplySystemAccent(string? hex, bool showOnStartAndTaskbar)
    {
        if (!TryParseAccent(hex, out var accentAbgr, out var r, out var g, out var b))
        {
            return false;
        }

        var changed = false;

        // Stop "pick accent from wallpaper"
        using (var desktop = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
        {
            changed |= SetDword(desktop, "AutoColorization", 0);
        }

        using (var personalize = Registry.CurrentUser.CreateSubKey(PersonalizeKey))
        {
            changed |= SetDword(personalize, "ColorPrevalence", showOnStartAndTaskbar ? 1u : 0u);
        }

        using (var dwm = Registry.CurrentUser.CreateSubKey(DwmKey))
        {
            changed |= SetDword(dwm, "AccentColor", accentAbgr);
            changed |= SetDword(dwm, "ColorizationColor", accentAbgr);
            changed |= SetDword(dwm, "ColorizationAfterglow", accentAbgr);
            changed |= SetDword(dwm, "ColorPrevalence", showOnStartAndTaskbar ? 1u : 0u);
            changed |= SetDword(dwm, "EnableWindowColorization", showOnStartAndTaskbar ? 1u : 0u);
        }

        using (var accent = Registry.CurrentUser.CreateSubKey(ExplorerAccentKey))
        {
            var menuChanged = SetDword(accent, "AccentColorMenu", accentAbgr);
            changed |= menuChanged;
            changed |= SetDword(accent, "StartColorMenu", accentAbgr);
            changed |= SetDword(accent, "UseNewAutoColorAccentAlgorithm", 0);
            if (menuChanged)
            {
                accent.SetValue("AccentPalette", BuildAccentPalette(r, g, b), RegistryValueKind.Binary);
                changed = true;
            }
        }

        if (changed)
        {
            BroadcastThemeChange();
        }

        return changed;
    }

    /// <summary>
    /// Stop overriding accent: turn off colored title bars/borders, clear prevalence,
    /// and let Windows pick accent from wallpaper again (AutoColorization=1).
    /// </summary>
    public bool RestoreSystemAccentDefaults()
    {
        var changed = false;

        using (var desktop = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
        {
            changed |= SetDword(desktop, "AutoColorization", 1);
        }

        using (var personalize = Registry.CurrentUser.CreateSubKey(PersonalizeKey))
        {
            changed |= SetDword(personalize, "ColorPrevalence", 0);
        }

        using (var dwm = Registry.CurrentUser.CreateSubKey(DwmKey))
        {
            // This is what makes Explorer / window frames glow with the accent.
            changed |= SetDword(dwm, "EnableWindowColorization", 0);
            changed |= SetDword(dwm, "ColorPrevalence", 0);
        }

        if (changed)
        {
            BroadcastThemeChange();
        }

        return changed;
    }

    public bool ApplyScheduled(AppConfig config, DateTime? now = null)
    {
        if (!config.Schedule.Enabled)
        {
            return false;
        }

        var desired = GetDesiredMode(config.Schedule, now);
        return ApplyTheme(desired, config.Theme);
    }

    private static bool SetDword(RegistryKey key, string name, uint value, bool force = false)
    {
        if (!force)
        {
            var current = key.GetValue(name);
            if (current is int i && unchecked((uint)i) == value)
            {
                return false;
            }

            if (current is long l && unchecked((uint)l) == value)
            {
                return false;
            }
        }

        key.SetValue(name, unchecked((int)value), RegistryValueKind.DWord);
        return true;
    }

    private static TimeSpan ParseTime(string hhmm)
    {
        if (TimeSpan.TryParse(hhmm, out var ts))
        {
            return new TimeSpan(ts.Hours, ts.Minutes, 0);
        }

        return TimeSpan.Zero;
    }

    /// <summary>Parse #AARRGGBB / #RRGGBB → Windows ABGR DWORD + RGB bytes.</summary>
    private static bool TryParseAccent(string? hex, out uint abgr, out byte r, out byte g, out byte b)
    {
        abgr = 0;
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        var s = hex.Trim().TrimStart('#');
        if (s.Length == 6)
        {
            s = "FF" + s;
        }

        if (s.Length != 8 || !uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var argb))
        {
            return false;
        }

        var a = (byte)((argb >> 24) & 0xFF);
        r = (byte)((argb >> 16) & 0xFF);
        g = (byte)((argb >> 8) & 0xFF);
        b = (byte)(argb & 0xFF);
        if (a == 0)
        {
            a = 0xFF;
        }

        abgr = ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
        return true;
    }

    /// <summary>8 Immersive accent shades as BGRA (32 bytes) for Explorer\Accent.</summary>
    private static byte[] BuildAccentPalette(byte r, byte g, byte b)
    {
        // Light3, Light2, Light1, Base, Dark1, Dark2, Dark3, Extra dark
        var factors = new (double TowardWhite, double TowardBlack)[]
        {
            (0.75, 0),
            (0.50, 0),
            (0.25, 0),
            (0, 0),
            (0, 0.20),
            (0, 0.40),
            (0, 0.60),
            (0, 0.80),
        };

        var palette = new byte[32];
        for (var i = 0; i < 8; i++)
        {
            var (tw, tb) = factors[i];
            var rr = (byte)Math.Clamp(r + (255 - r) * tw - r * tb, 0, 255);
            var gg = (byte)Math.Clamp(g + (255 - g) * tw - g * tb, 0, 255);
            var bb = (byte)Math.Clamp(b + (255 - b) * tw - b * tb, 0, 255);
            var o = i * 4;
            palette[o] = bb;
            palette[o + 1] = gg;
            palette[o + 2] = rr;
            palette[o + 3] = 0xFF;
        }

        return palette;
    }

    private static void BroadcastThemeChange()
    {
        // Non-blocking: SendMessageTimeout(HWND_BROADCAST) waits on every top-level
        // window and is a common cause of "theme switch hitch" on busy desktops.
        NativeMethods.SendNotifyMessage(
            (IntPtr)NativeMethods.HWND_BROADCAST,
            NativeMethods.WM_SETTINGCHANGE,
            UIntPtr.Zero,
            "ImmersiveColorSet");
    }
}

