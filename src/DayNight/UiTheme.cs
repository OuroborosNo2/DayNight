using System.Windows;
using System.Windows.Media;
using DayNight.Core.Services;
using MediaColor = System.Windows.Media.Color;

namespace DayNight;

/// <summary>
/// DayNight window palette. Light/dark base + optional custom accent for app chrome.
/// </summary>
internal static class UiTheme
{
    private static MediaColor? _customAccent;
    private static ThemeMode _lastMode = ThemeMode.Dark;

    public static void SetCustomAccent(MediaColor? accent)
    {
        _customAccent = accent;
        Apply(_lastMode);
    }

    public static void Apply(ThemeMode mode)
    {
        _lastMode = mode;
        var app = System.Windows.Application.Current;
        if (app?.Resources is null)
        {
            return;
        }

        EnsureBrush(app, "Bg");
        EnsureBrush(app, "Panel");
        EnsureBrush(app, "PanelElevated");
        EnsureBrush(app, "Stroke");
        EnsureBrush(app, "Text");
        EnsureBrush(app, "Muted");
        EnsureBrush(app, "Accent");
        EnsureBrush(app, "AccentSoft");
        EnsureBrush(app, "GhostBg");
        EnsureBrush(app, "NavHover");
        EnsureBrush(app, "FieldBg");
        EnsureBrush(app, "LogoBg");
        EnsureBrush(app, "Sun");
        EnsureBrush(app, "Moon");
        EnsureBrush(app, "SwatchBorder");
        EnsureBrush(app, "Good");
        EnsureBrush(app, "PopupBg");

        if (mode == ThemeMode.Light)
        {
            Paint(app, "Bg", 243, 241, 236);
            Paint(app, "Panel", 255, 252, 248);
            Paint(app, "PanelElevated", 255, 255, 255);
            Paint(app, "Stroke", 228, 221, 210);
            Paint(app, "Text", 28, 25, 23);
            Paint(app, "Muted", 120, 113, 108);
            Paint(app, "GhostBg", 245, 240, 232);
            Paint(app, "NavHover", 240, 235, 227);
            Paint(app, "FieldBg", 255, 255, 255);
            Paint(app, "PopupBg", 255, 255, 255);
            Paint(app, "LogoBg", 20, 184, 166);
            Paint(app, "Sun", 251, 191, 36);
            Paint(app, "Moon", 255, 247, 237);
            Paint(app, "SwatchBorder", 0, 0, 0, 0x33);
            Paint(app, "Good", 5, 150, 105);
            ApplyAccentColors(app, lightMode: true);
        }
        else
        {
            Paint(app, "Bg", 17, 24, 39);
            Paint(app, "Panel", 31, 41, 55);
            Paint(app, "PanelElevated", 39, 52, 73);
            Paint(app, "Stroke", 55, 65, 81);
            Paint(app, "Text", 243, 244, 246);
            Paint(app, "Muted", 176, 186, 201);
            Paint(app, "GhostBg", 51, 65, 85);
            Paint(app, "NavHover", 42, 54, 72);
            Paint(app, "FieldBg", 15, 23, 42);
            Paint(app, "PopupBg", 15, 23, 42);
            Paint(app, "LogoBg", 15, 118, 110);
            Paint(app, "Sun", 251, 191, 36);
            Paint(app, "Moon", 224, 242, 254);
            Paint(app, "SwatchBorder", 255, 255, 255, 0x55);
            Paint(app, "Good", 52, 211, 153);
            ApplyAccentColors(app, lightMode: false);
        }
    }

    private static void ApplyAccentColors(System.Windows.Application app, bool lightMode)
    {
        MediaColor accent;
        MediaColor soft;
        if (_customAccent is MediaColor custom)
        {
            accent = custom;
            soft = lightMode
                ? Mix(custom, MediaColor.FromRgb(255, 255, 255), 0.72)
                : Mix(custom, MediaColor.FromRgb(15, 23, 42), 0.55);
        }
        else if (lightMode)
        {
            accent = MediaColor.FromRgb(13, 148, 136);
            soft = MediaColor.FromRgb(204, 251, 241);
        }
        else
        {
            accent = MediaColor.FromRgb(45, 212, 191);
            soft = MediaColor.FromRgb(19, 78, 74);
        }

        Paint(app, "Accent", accent.R, accent.G, accent.B, accent.A);
        Paint(app, "AccentSoft", soft.R, soft.G, soft.B, soft.A);
    }

    private static MediaColor Mix(MediaColor a, MediaColor b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return MediaColor.FromArgb(
            255,
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }

    private static void EnsureBrush(System.Windows.Application app, string key)
    {
        if (app.Resources[key] is SolidColorBrush brush)
        {
            if (!brush.IsFrozen)
            {
                return;
            }

            var clone = brush.Clone();
            app.Resources.Remove(key);
            app.Resources.Add(key, clone);
            return;
        }

        if (app.Resources.Contains(key))
        {
            app.Resources.Remove(key);
        }

        app.Resources.Add(key, new SolidColorBrush(MediaColor.FromRgb(0, 0, 0)));
    }

    private static void Paint(System.Windows.Application app, string key, byte r, byte g, byte b, byte a = 255)
    {
        if (app.Resources[key] is SolidColorBrush brush && !brush.IsFrozen)
        {
            brush.Color = MediaColor.FromArgb(a, r, g, b);
            return;
        }

        if (app.Resources.Contains(key))
        {
            app.Resources.Remove(key);
        }

        app.Resources.Add(key, new SolidColorBrush(MediaColor.FromArgb(a, r, g, b)));
    }
}
