using DayNight.Core.Models;

namespace DayNight.Core.Services;

public sealed class DayNightEngine : IDisposable
{
    private readonly ConfigStore _configStore;
    private readonly ThemeService _theme = new();
    private readonly GammaService _gamma = new();
    private readonly CabCService _cabc = new();
    private readonly AutostartService _autostart = new();
    private readonly LocalizationService _i18n;
    private System.Threading.Timer? _timer;
    private AppConfig _config;
    private ThemeMode? _manualHold;
    private readonly object _gate = new();

    public DayNightEngine(string? rootDirectory = null, string? localesDirectory = null)
    {
        _configStore = new ConfigStore(rootDirectory);
        _i18n = new LocalizationService(localesDirectory);
        _config = _configStore.LoadOrCreate();
        _i18n.ApplyLanguagePreference(_config.Language);
    }

    public AppConfig Config
    {
        get
        {
            lock (_gate)
            {
                return _config;
            }
        }
    }

    public LocalizationService I18n => _i18n;
    public ThemeService Theme => _theme;
    public GammaService Gamma => _gamma;
    public CabCService CabC => _cabc;
    public AutostartService Autostart => _autostart;
    public string ConfigPath => _configStore.ConfigPath;

    public event EventHandler? ConfigApplied;
    public event EventHandler<ThemeMode>? UiThemeChanged;

    public void Start()
    {
        ApplyAll(forceGamma: true, notifyUi: true);
        var seconds = Math.Max(5, _config.General.CheckIntervalSeconds);
        _timer = new System.Threading.Timer(_ => Tick(), null, TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));
    }

    public void SaveAndApply(AppConfig config, bool elevateHintForCabc = false)
    {
        lock (_gate)
        {
            _config = config;
            _configStore.Save(config);
            _i18n.ApplyLanguagePreference(config.Language);
        }

        var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "DayNight.exe");
        _autostart.SetEnabled(config.General.StartWithWindows, exe);
        ApplyAll(forceGamma: true, manageCabc: config.Display.ManageCabc, notifyUi: true);
        ConfigApplied?.Invoke(this, EventArgs.Empty);
        _ = elevateHintForCabc;
    }

    public void Reload()
    {
        lock (_gate)
        {
            _config = _configStore.LoadOrCreate();
            _i18n.ApplyLanguagePreference(_config.Language);
        }

        ApplyAll(forceGamma: true, notifyUi: true);
        ConfigApplied?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyManualTheme(ThemeMode mode, bool notifyUi = true)
    {
        AppConfig cfg;
        lock (_gate)
        {
            _manualHold = mode;
            cfg = _config;
            _theme.ApplyTheme(mode, cfg.Theme);
        }

        if (notifyUi)
        {
            UiThemeChanged?.Invoke(this, mode);
        }
    }

    /// <summary>Persist accent into config and push it to Windows + optional chrome flags.</summary>
    public void ApplyAccentNow(string accentColor, bool showOnStartAndTaskbar, bool persist = true)
    {
        lock (_gate)
        {
            _config.Theme.ApplyAccent = true;
            _config.Theme.AccentColor = accentColor;
            _config.Theme.ShowAccentOnStartAndTaskbar = showOnStartAndTaskbar;
            if (persist)
            {
                _configStore.Save(_config);
            }
        }

        _theme.ApplySystemAccent(accentColor, showOnStartAndTaskbar);
    }

    /// <summary>Stop managing accent; restore Windows defaults (no colored window frames).</summary>
    public void RestoreAccentDefaults(bool persist = true)
    {
        lock (_gate)
        {
            _config.Theme.ApplyAccent = false;
            _config.Theme.ShowAccentOnStartAndTaskbar = false;
            if (persist)
            {
                _configStore.Save(_config);
            }
        }

        _theme.RestoreSystemAccentDefaults();
    }

    public CabCResult ApplyCabcFromConfig()
    {
        AppConfig cfg;
        lock (_gate)
        {
            cfg = _config;
        }

        if (!cfg.Display.ManageCabc)
        {
            return new CabCResult { Success = true, Message = "not_managed" };
        }

        return _cabc.Apply(CabCService.ParseMode(cfg.Display.CabcMode));
    }

    public ThemeMode CurrentDesiredTheme()
    {
        lock (_gate)
        {
            if (_manualHold is ThemeMode hold)
            {
                return hold;
            }

            return ResolveScheduledOrSystem(_config);
        }
    }

    private void Tick()
    {
        try
        {
            ApplyAll(forceGamma: false, notifyUi: true);
        }
        catch
        {
            // keep timer alive
        }
    }

    private void ApplyAll(bool forceGamma, bool manageCabc = false, bool notifyUi = false)
    {
        ThemeMode mode;
        AppConfig cfg;

        lock (_gate)
        {
            cfg = _config;
            if (_manualHold is ThemeMode manual)
            {
                var scheduled = ResolveScheduledOrSystem(cfg);
                if (cfg.Schedule.Enabled && scheduled == manual)
                {
                    _manualHold = null;
                    mode = scheduled;
                }
                else
                {
                    mode = manual;
                }
            }
            else if (cfg.Schedule.Enabled)
            {
                mode = _theme.GetDesiredMode(cfg.Schedule);
            }
            else
            {
                mode = _theme.GetCurrentAppTheme();
            }

            _theme.ApplyTheme(mode, cfg.Theme);

            // Accent only when enabled — skipped on pure light/dark toggles inside ApplyTheme.
            // ApplySystemAccent no-ops (no broadcast) when values are already current.
            if (cfg.Theme.ApplyAccent)
            {
                _theme.ApplySystemAccent(cfg.Theme.AccentColor, cfg.Theme.ShowAccentOnStartAndTaskbar);
            }

            if (cfg.Display.GammaEnabled || forceGamma)
            {
                _gamma.ApplyFromConfig(cfg.Display);
            }

            if (manageCabc && cfg.Display.ManageCabc)
            {
                _cabc.Apply(CabCService.ParseMode(cfg.Display.CabcMode));
            }
        }

        if (notifyUi)
        {
            UiThemeChanged?.Invoke(this, mode);
        }
    }

    private ThemeMode ResolveScheduledOrSystem(AppConfig cfg)
        => cfg.Schedule.Enabled
            ? _theme.GetDesiredMode(cfg.Schedule)
            : _theme.GetCurrentAppTheme();

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
