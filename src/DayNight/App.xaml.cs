using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using DayNight.Core.Services;

namespace DayNight;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Local\\DayNight.SingleInstance.v1";
    private Mutex? _mutex;
    private DayNightEngine? _engine;
    private TrayController? _tray;
    private MainWindow? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out var createdNew);
        if (!createdNew)
        {
            TryActivateExistingWindow();
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var root = AppContext.BaseDirectory;
        _engine = new DayNightEngine(root, Path.Combine(root, "locales"));

        ApplyUiFromEngine(_engine);
        _engine.Start();

        _main = new MainWindow(_engine);
        _tray = new TrayController(_engine, _main);

        // BeginInvoke: never block timer/worker threads on the UI dispatcher (avoids tray/UI deadlocks).
        _engine.UiThemeChanged += (_, mode) => Dispatcher.BeginInvoke(() =>
        {
            ApplyUi(mode);
            _main?.RefreshChromeFromTheme();
        });
        _engine.ConfigApplied += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            ApplyUiFromEngine(_engine!);
            _main?.RefreshChromeFromTheme();
            _main?.ReloadLocalization();
        });

        if (_engine.Config.General.StartMinimized)
        {
            _main.Hide();
        }
        else
        {
            _main.Show();
            _main.WindowState = WindowState.Normal;
            _main.Activate();
        }
    }

    private static void ApplyUiFromEngine(DayNightEngine engine)
        => ApplyUi(engine.CurrentDesiredTheme());

    private static void ApplyUi(ThemeMode mode)
        => UiTheme.Apply(mode);

    private static void TryActivateExistingWindow()
    {
        var found = IntPtr.Zero;
        EnumWindows((hWnd, _) =>
        {
            var len = GetWindowTextLength(hWnd);
            if (len <= 0)
            {
                return true;
            }

            var sb = new System.Text.StringBuilder(len + 1);
            _ = GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();
            if (title.Contains("DayNight", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("昼夜", StringComparison.Ordinal))
            {
                found = hWnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        if (found == IntPtr.Zero)
        {
            return;
        }

        ShowWindow(found, 9); // SW_RESTORE
        SetForegroundWindow(found);
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _engine?.Dispose();
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch
        {
            // ignore
        }

        _mutex?.Dispose();
        base.OnExit(e);
    }
}
