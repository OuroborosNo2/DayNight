using DayNight.Core.Native;

namespace DayNight.Core.Services;

/// <summary>
/// Software contrast / brightness via Win32 gamma ramp (no panel OSD required).
/// Effect resets after reboot or some full-screen exclusive apps — DayNight re-applies on a timer.
/// </summary>
public sealed class GammaService
{
    public bool Apply(double contrast, double brightness, double gamma)
    {
        contrast = Math.Clamp(contrast, 0.5, 2.0);
        brightness = Math.Clamp(brightness, -0.5, 0.5);
        gamma = Math.Clamp(gamma, 0.4, 2.5);

        var ramp = BuildRamp(contrast, brightness, gamma);
        var hdc = NativeMethods.GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return NativeMethods.SetDeviceGammaRamp(hdc, ref ramp);
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
        }
    }

    public bool Reset()
        => Apply(contrast: 1.0, brightness: 0.0, gamma: 1.0);

    public bool ApplyFromConfig(Models.DisplayConfig display)
    {
        if (!display.GammaEnabled)
        {
            return Reset();
        }

        return Apply(display.Contrast, display.Brightness, display.Gamma);
    }

    private static NativeMethods.RAMP BuildRamp(double contrast, double brightness, double gamma)
    {
        var red = new ushort[256];
        var green = new ushort[256];
        var blue = new ushort[256];

        for (var i = 0; i < 256; i++)
        {
            var normalized = i / 255.0;
            var centered = (normalized - 0.5) * contrast + 0.5 + brightness;
            centered = Math.Clamp(centered, 0.0, 1.0);
            var curved = Math.Pow(centered, 1.0 / Math.Max(gamma, 0.01));
            var value = (ushort)Math.Clamp((int)Math.Round(curved * 65535.0), 0, 65535);
            red[i] = green[i] = blue[i] = value;
        }

        return new NativeMethods.RAMP
        {
            Red = red,
            Green = green,
            Blue = blue
        };
    }
}
