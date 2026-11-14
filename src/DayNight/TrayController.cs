using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using DayNight.Core.Services;

namespace DayNight;

internal sealed class TrayController : IDisposable
{
    private readonly DayNightEngine _engine;
    private readonly MainWindow _window;
    private readonly NotifyIcon _notify;

    public TrayController(DayNightEngine engine, MainWindow window)
    {
        _engine = engine;
        _window = window;

        _notify = new NotifyIcon
        {
            Visible = true,
            Text = "DayNight",
            Icon = LoadIcon()
        };

        _notify.DoubleClick += (_, _) => ShowWindow();
        RebuildMenu();
        _engine.I18n.LanguageChanged += (_, _) => RebuildMenu();
    }

    private void RebuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_engine.I18n.T("tray.open"), null, (_, _) => ShowWindow());
        menu.Items.Add(_engine.I18n.T("tray.light"), null, (_, _) =>
        {
            _engine.ApplyManualTheme(ThemeMode.Light);
        });
        menu.Items.Add(_engine.I18n.T("tray.dark"), null, (_, _) =>
        {
            _engine.ApplyManualTheme(ThemeMode.Dark);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_engine.I18n.T("tray.exit"), null, (_, _) =>
        {
            _notify.Visible = false;
            System.Windows.Application.Current.Shutdown();
        });
        _notify.ContextMenuStrip = menu;
        _notify.Text = _engine.I18n.T("app.name");
    }

    private void ShowWindow()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private static Icon LoadIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/DayNight.ico");
            var streamInfo = System.Windows.Application.GetResourceStream(uri);
            if (streamInfo?.Stream is not null)
            {
                return new Icon(streamInfo.Stream);
            }
        }
        catch
        {
            // fall through
        }

        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(exe))
            {
                var fromExe = Icon.ExtractAssociatedIcon(exe);
                if (fromExe is not null)
                {
                    return fromExe;
                }
            }
        }
        catch
        {
            // fall through
        }

        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
    }
}
