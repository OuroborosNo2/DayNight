using Microsoft.Win32;

namespace DayNight.Core.Services;

public enum CabcMode
{
    /// <summary>Do not manage; leave Windows as-is.</summary>
    System = -1,
    Off = 0,
    Always = 1,
    Battery = 2
}

public sealed class CabCResult
{
    public bool Success { get; init; }
    public bool RequiresElevation { get; init; }
    public bool MayNeedReboot { get; init; }
    public string Message { get; init; } = "";
    public CabcMode? Current { get; init; }
}

/// <summary>
/// Content Adaptive Brightness Control (Windows 11 "Change brightness based on content").
/// Writes HKLM\...\GraphicsDrivers\CABCOption — may require admin.
/// </summary>
public sealed class CabCService
{
    private const string KeyPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";

    public CabcMode? TryRead()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath);
            var value = key?.GetValue("CABCOption");
            if (value is int i && i is >= 0 and <= 2)
            {
                return (CabcMode)i;
            }

            // Legacy kill-switch
            var disabled = key?.GetValue("DisableCABC");
            if (disabled is int d && d == 1)
            {
                return CabcMode.Off;
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    public CabCResult Apply(CabcMode mode)
    {
        if (mode == CabcMode.System)
        {
            return new CabCResult
            {
                Success = true,
                Message = "left_to_system"
            };
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true)
                ?? Registry.LocalMachine.CreateSubKey(KeyPath, true);

            if (key is null)
            {
                return new CabCResult
                {
                    Success = false,
                    RequiresElevation = true,
                    Message = "cannot_open_key"
                };
            }

            key.SetValue("CABCOption", (int)mode, RegistryValueKind.DWord);
            if (mode == CabcMode.Off)
            {
                key.SetValue("DisableCABC", 1, RegistryValueKind.DWord);
            }
            else
            {
                key.SetValue("DisableCABC", 0, RegistryValueKind.DWord);
            }

            return new CabCResult
            {
                Success = true,
                MayNeedReboot = true,
                Current = mode,
                Message = "applied"
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new CabCResult
            {
                Success = false,
                RequiresElevation = true,
                Message = "needs_admin"
            };
        }
        catch (Exception ex)
        {
            return new CabCResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public static CabcMode ParseMode(string? value)
        => (value ?? "system").Trim().ToLowerInvariant() switch
        {
            "off" => CabcMode.Off,
            "always" => CabcMode.Always,
            "battery" => CabcMode.Battery,
            _ => CabcMode.System
        };

    public static string ToConfigValue(CabcMode mode)
        => mode switch
        {
            CabcMode.Off => "off",
            CabcMode.Always => "always",
            CabcMode.Battery => "battery",
            _ => "system"
        };
}
