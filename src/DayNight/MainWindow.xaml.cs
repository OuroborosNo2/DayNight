using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DayNight.Core.Models;
using DayNight.Core.Services;

namespace DayNight;

public partial class MainWindow : Window
{
    private static readonly (string Hex, string Zh, string En)[] AccentPresets =
    [
        ("#FF3B82F6", "天蓝", "Sky"),
        ("#FF0EA5E9", "青蓝", "Cyan"),
        ("#FF14B8A6", "青绿", "Teal"),
        ("#FF22C55E", "翠绿", "Green"),
        ("#FFF59E0B", "琥珀", "Amber"),
        ("#FFF97316", "橙色", "Orange"),
        ("#FFEF4444", "红色", "Red"),
        ("#FFEC4899", "粉色", "Pink"),
        ("#FFA855F7", "紫色", "Purple"),
        ("#FF64748B", "石板灰", "Slate"),
    ];

    private readonly DayNightEngine _engine;
    private bool _suspendAccentPush;

    public MainWindow(DayNightEngine engine)
    {
        InitializeComponent();
        _engine = engine;
        BuildPresetSwatches();
        ApplyAccent.Checked += ApplyAccent_OnChanged;
        ApplyAccent.Unchecked += ApplyAccent_OnChanged;
        AccentOnChrome.Checked += ApplyAccent_OnChanged;
        AccentOnChrome.Unchecked += ApplyAccent_OnChanged;
        LoadFromConfig(_engine.Config);
        ApplyLocalization();
        _engine.I18n.LanguageChanged += (_, _) => Dispatcher.Invoke(ApplyLocalization);

        ContrastSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        BrightnessSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        GammaSlider.ValueChanged += (_, _) => UpdateSliderLabels();
        UpdateSliderLabels();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    private void BuildPresetSwatches()
    {
        PresetPanel.Children.Clear();
        foreach (var preset in AccentPresets)
        {
            var btn = new System.Windows.Controls.Button
            {
                Style = (Style)FindResource("Swatch"),
                Background = BrushFromHex(preset.Hex),
                ToolTip = $"{preset.Zh} / {preset.En}",
                Tag = preset.Hex
            };
            btn.Click += (_, _) =>
            {
                AccentBox.Text = (string)btn.Tag;
                ApplyAccent.IsChecked = true;
                UpdateAccentPreview(AccentBox.Text);
            };
            PresetPanel.Children.Add(btn);
        }
    }

    private void Nav_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        PageSchedule.Visibility = NavSchedule.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PageTheme.Visibility = NavTheme.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PageDisplay.Visibility = NavDisplay.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PageGeneral.Visibility = NavGeneral.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PageAbout.Visibility = NavAbout.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ManualLight_OnClick(object sender, RoutedEventArgs e)
        => ApplyManualUi(ThemeMode.Light);

    private void ManualDark_OnClick(object sender, RoutedEventArgs e)
        => ApplyManualUi(ThemeMode.Dark);

    private void ApplyManualUi(ThemeMode mode)
    {
        // System theme broadcast can scramble WPF chrome — apply system first, then paint.
        _engine.ApplyManualTheme(mode, notifyUi: false);
        UiTheme.Apply(mode);
        PaintWindowChrome(mode);
        StatusText.Text = mode == ThemeMode.Light
            ? _engine.I18n.T("status.manual_light")
            : _engine.I18n.T("status.manual_dark");

        // Re-paint after DWM/theme broadcast settles.
        Dispatcher.BeginInvoke(() =>
        {
            UiTheme.Apply(mode);
            PaintWindowChrome(mode);
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void PaintWindowChrome(ThemeMode mode)
    {
        var elev = mode == ThemeMode.Light
            ? System.Windows.Media.Color.FromRgb(255, 255, 255)
            : System.Windows.Media.Color.FromRgb(39, 52, 73);
        var panel = mode == ThemeMode.Light
            ? System.Windows.Media.Color.FromRgb(255, 252, 248)
            : System.Windows.Media.Color.FromRgb(31, 41, 55);
        var bg = mode == ThemeMode.Light
            ? System.Windows.Media.Color.FromRgb(243, 241, 236)
            : System.Windows.Media.Color.FromRgb(17, 24, 39);
        var text = mode == ThemeMode.Light
            ? System.Windows.Media.Color.FromRgb(28, 25, 23)
            : System.Windows.Media.Color.FromRgb(243, 244, 246);
        var muted = mode == ThemeMode.Light
            ? System.Windows.Media.Color.FromRgb(120, 113, 108)
            : System.Windows.Media.Color.FromRgb(156, 163, 175);
        var stroke = mode == ThemeMode.Light
            ? System.Windows.Media.Color.FromRgb(228, 221, 210)
            : System.Windows.Media.Color.FromRgb(55, 65, 81);

        Background = new SolidColorBrush(bg);
        SidebarBorder.Background = new SolidColorBrush(panel);
        SidebarBorder.BorderBrush = new SolidColorBrush(stroke);
        ContentBorder.Background = new SolidColorBrush(panel);
        ContentBorder.BorderBrush = new SolidColorBrush(stroke);

        var cardStyle = TryFindResource("Card") as Style;
        foreach (var border in FindVisualChildren<Border>(ContentBorder))
        {
            if (cardStyle is null || !Equals(border.Style, cardStyle))
            {
                continue;
            }

            border.Background = new SolidColorBrush(elev);
            border.BorderBrush = new SolidColorBrush(stroke);
        }

        TitleText.Foreground = new SolidColorBrush(text);
        TitleEnText.Foreground = new SolidColorBrush(muted);
        StatusText.Foreground = new SolidColorBrush(muted);
        VersionText.Foreground = new SolidColorBrush(muted);
        InvalidateVisual();
        UpdateLayout();
    }

    public void RefreshChromeFromTheme()
    {
        PaintWindowChrome(_engine.CurrentDesiredTheme());
    }

    public void ReloadLocalization() => ApplyLocalization();

    private void RefreshChromeBrushes()
    {
        System.Windows.Media.Brush Bg() => (System.Windows.Media.Brush)FindResource("Bg");
        System.Windows.Media.Brush Panel() => (System.Windows.Media.Brush)FindResource("Panel");
        System.Windows.Media.Brush Elevated() => (System.Windows.Media.Brush)FindResource("PanelElevated");
        System.Windows.Media.Brush Stroke() => (System.Windows.Media.Brush)FindResource("Stroke");
        System.Windows.Media.Brush Text() => (System.Windows.Media.Brush)FindResource("Text");
        System.Windows.Media.Brush Muted() => (System.Windows.Media.Brush)FindResource("Muted");

        Background = Bg();
        SidebarBorder.Background = Panel();
        SidebarBorder.BorderBrush = Stroke();
        ContentBorder.Background = Panel();
        ContentBorder.BorderBrush = Stroke();

        var cardStyle = TryFindResource("Card") as Style;
        foreach (var border in FindVisualChildren<Border>(ContentBorder))
        {
            if (cardStyle is not null && Equals(border.Style, cardStyle))
            {
                border.Background = Elevated();
                border.BorderBrush = Stroke();
            }
        }

        TitleText.Foreground = Text();
        TitleEnText.Foreground = Muted();
        StatusText.Foreground = Muted();
        VersionText.Foreground = Muted();
        InvalidateVisual();
    }

    private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        if (parent is null)
        {
            yield break;
        }

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }

    private void LoadFromConfig(AppConfig cfg)
    {
        ScheduleEnabled.IsChecked = cfg.Schedule.Enabled;
        LightAtBox.Text = cfg.Schedule.LightAt;
        DarkAtBox.Text = cfg.Schedule.DarkAt;

        ChangeApps.IsChecked = cfg.Theme.ChangeApps;
        ChangeSystem.IsChecked = cfg.Theme.ChangeSystem;
        _suspendAccentPush = true;
        // Chrome flag first — ApplyAccent Checked handler reads AccentOnChrome.
        AccentOnChrome.IsChecked = cfg.Theme.ShowAccentOnStartAndTaskbar;
        AccentBox.Text = cfg.Theme.AccentColor;
        ApplyAccent.IsChecked = cfg.Theme.ApplyAccent;
        _suspendAccentPush = false;

        GammaEnabled.IsChecked = cfg.Display.GammaEnabled;
        ContrastSlider.Value = cfg.Display.Contrast;
        BrightnessSlider.Value = cfg.Display.Brightness;
        GammaSlider.Value = cfg.Display.Gamma;
        ManageCabc.IsChecked = cfg.Display.ManageCabc;

        StartWithWindows.IsChecked = cfg.General.StartWithWindows;
        StartMinimized.IsChecked = cfg.General.StartMinimized;

        RebuildLanguageBox(cfg.Language);
        RebuildCabcBox(cfg.Display.CabcMode);
        UpdateAccentPreview(cfg.Theme.AccentColor);
        if (cfg.Theme.ApplyAccent && TryParseColor(cfg.Theme.AccentColor, out var loadedAccent))
        {
            UiTheme.SetCustomAccent(loadedAccent);
        }
        else
        {
            UiTheme.SetCustomAccent(null);
        }
    }

    private AppConfig CaptureConfig()
    {
        var cfg = _engine.Config;
        cfg.Schedule.Enabled = ScheduleEnabled.IsChecked == true;
        cfg.Schedule.LightAt = LightAtBox.Text.Trim();
        cfg.Schedule.DarkAt = DarkAtBox.Text.Trim();

        cfg.Theme.ChangeApps = ChangeApps.IsChecked == true;
        cfg.Theme.ChangeSystem = ChangeSystem.IsChecked == true;
        cfg.Theme.ApplyAccent = ApplyAccent.IsChecked == true;
        cfg.Theme.AccentColor = AccentBox.Text.Trim();
        cfg.Theme.ShowAccentOnStartAndTaskbar = AccentOnChrome.IsChecked == true;

        cfg.Display.GammaEnabled = GammaEnabled.IsChecked == true;
        cfg.Display.Contrast = ContrastSlider.Value;
        cfg.Display.Brightness = BrightnessSlider.Value;
        cfg.Display.Gamma = GammaSlider.Value;
        cfg.Display.ManageCabc = ManageCabc.IsChecked == true;
        cfg.Display.CabcMode = CabcModeBox.SelectedItem is ComboItem ci ? ci.Value : "system";

        cfg.General.StartWithWindows = StartWithWindows.IsChecked == true;
        cfg.General.StartMinimized = StartMinimized.IsChecked == true;
        cfg.Language = LanguageBox.SelectedItem is ComboItem li ? li.Value : "auto";
        return cfg;
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        var cfg = CaptureConfig();
        if (cfg.Theme.ApplyAccent && TryParseColor(cfg.Theme.AccentColor, out var accent))
        {
            UiTheme.SetCustomAccent(accent);
            _engine.SaveAndApply(cfg);
        }
        else
        {
            UiTheme.SetCustomAccent(null);
            cfg.Theme.ApplyAccent = false;
            cfg.Theme.ShowAccentOnStartAndTaskbar = false;
            _engine.SaveAndApply(cfg);
            _engine.RestoreAccentDefaults(persist: true);
        }

        StatusText.Text = _engine.I18n.T("status.saved");
        ApplyLocalization();
        PaintWindowChrome(_engine.CurrentDesiredTheme());
    }

    private void Picker_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            SolidColorOnly = false
        };

        if (TryParseColor(AccentBox.Text, out var current))
        {
            dialog.Color = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B);
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var c = dialog.Color;
        AccentBox.Text = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        ApplyAccent.IsChecked = true;
        UpdateAccentPreview(AccentBox.Text);
    }

    private void AccentBox_OnTextChanged(object sender, TextChangedEventArgs e)
        => UpdateAccentPreview(AccentBox.Text);

    private void ApplyCabc_OnClick(object sender, RoutedEventArgs e)
    {
        var cfg = CaptureConfig();
        _engine.SaveAndApply(cfg);
        var result = _engine.ApplyCabcFromConfig();
        if (result.RequiresElevation)
        {
            StatusText.Text = _engine.I18n.T("display.cabc_needs_admin");
            TryRelaunchElevated();
            return;
        }

        StatusText.Text = result.MayNeedReboot
            ? _engine.I18n.T("display.cabc_reboot")
            : _engine.I18n.T("display.cabc_ok");
    }

    private void OpenDisplaySettings_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private void PresetNeutral_OnClick(object sender, RoutedEventArgs e)
        => SetGammaPreset(1.0, 0.0, 1.0);

    private void PresetComfort_OnClick(object sender, RoutedEventArgs e)
        => SetGammaPreset(1.08, 0.02, 1.05);

    private void PresetCrisp_OnClick(object sender, RoutedEventArgs e)
        => SetGammaPreset(1.18, 0.0, 0.95);

    private void ResetGamma_OnClick(object sender, RoutedEventArgs e)
    {
        _engine.Gamma.Reset();
        SetGammaPreset(1.0, 0.0, 1.0);
        GammaEnabled.IsChecked = false;
    }

    private void SetGammaPreset(double contrast, double brightness, double gamma)
    {
        ContrastSlider.Value = contrast;
        BrightnessSlider.Value = brightness;
        GammaSlider.Value = gamma;
        GammaEnabled.IsChecked = true;
    }

    private void UpdateSliderLabels()
    {
        if (ContrastLabel is null)
        {
            return;
        }

        ContrastLabel.Text = $"{_engine.I18n.T("display.contrast")}: {ContrastSlider.Value:0.00}";
        BrightnessLabel.Text = $"{_engine.I18n.T("display.brightness")}: {BrightnessSlider.Value:0.00}";
        GammaLabel.Text = $"{_engine.I18n.T("display.gamma")}: {GammaSlider.Value:0.00}";
    }

    private void UpdateAccentPreview(string? hex)
    {
        if (AccentPreview is null)
        {
            return;
        }

        if (TryParseColor(hex, out var c))
        {
            AccentPreview.Background = new SolidColorBrush(c);
            if (ApplyAccent?.IsChecked == true)
            {
                UiTheme.SetCustomAccent(c);
                if (!_suspendAccentPush)
                {
                    // Push Windows accent (Explorer\Accent + DWM), not only this window.
                    _engine.ApplyAccentNow(
                        AccentBox.Text.Trim(),
                        AccentOnChrome.IsChecked == true,
                        persist: false);
                }
            }
        }
        else
        {
            AccentPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6));
        }
    }

    private void ApplyAccent_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_suspendAccentPush)
        {
            return;
        }

        if (ApplyAccent.IsChecked == true && TryParseColor(AccentBox.Text, out var c))
        {
            UiTheme.SetCustomAccent(c);
            _engine.ApplyAccentNow(
                AccentBox.Text.Trim(),
                AccentOnChrome.IsChecked == true,
                persist: false);
        }
        else if (ApplyAccent.IsChecked != true)
        {
            UiTheme.SetCustomAccent(null);
            // Drop colored Explorer/title-bar frames and hand accent back to Windows.
            _engine.RestoreAccentDefaults(persist: false);
        }
    }

    private void ApplyLocalization()
    {
        var t = _engine.I18n.T;
        Title = $"{t("app.name")} {t("app.name.en")}";
        TitleText.Text = t("app.name");
        TitleEnText.Text = t("app.name.en");
        TaglineText.Text = t("app.tagline");
        NavSchedule.Content = t("nav.schedule");
        NavTheme.Content = t("nav.theme");
        NavDisplay.Content = t("nav.display");
        NavGeneral.Content = t("nav.general");
        NavAbout.Content = t("nav.about");
        LiteBadge.Text = t("app.lite_badge");
        ManualTitle.Text = t("manual.title");
        ManualHint.Text = t("manual.hint");
        ManualLight.Content = t("manual.light");
        ManualDark.Content = t("manual.dark");
        ScheduleTitle.Text = t("nav.schedule");
        ScheduleEnabled.Content = t("schedule.enabled");
        LightAtLabel.Text = t("schedule.light_at");
        DarkAtLabel.Text = t("schedule.dark_at");
        ScheduleHint.Text = t("schedule.hint");
        ModeTitle.Text = t("theme.mode_title");
        ModeHint.Text = t("theme.mode_hint");
        ChangeApps.Content = t("theme.change_apps");
        ChangeSystem.Content = t("theme.change_system");
        AccentTitle.Text = t("theme.accent_title");
        AccentHint.Text = t("theme.accent_hint");
        ApplyAccent.Content = t("theme.apply_accent");
        AccentOnChrome.Content = t("theme.accent_on_chrome");
        PresetsLabel.Text = t("theme.presets");
        AccentLabel.Text = t("theme.accent_color");
        PickerButton.Content = t("theme.picker");
        GammaEnabled.Content = t("display.gamma_enabled");
        PresetNeutral.Content = t("display.preset_neutral");
        PresetComfort.Content = t("display.preset_comfort");
        PresetCrisp.Content = t("display.preset_crisp");
        ResetGamma.Content = t("btn.reset_gamma");
        GammaHint.Text = t("display.gamma_hint");
        CabcSection.Text = t("display.cabc_section");
        ManageCabc.Content = t("display.cabc_manage");
        CabcModeLabel.Text = t("display.cabc_mode");
        CabcHint.Text = t("display.cabc_hint");
        ApplyCabcButton.Content = t("display.cabc_apply");
        OpenDisplaySettingsButton.Content = t("display.open_settings");
        LanguageLabel.Text = t("general.language");
        StartWithWindows.Content = t("general.start_with_windows");
        StartMinimized.Content = t("general.start_minimized");
        PortableHint.Text = t("general.portable_hint");
        AboutTitle.Text = t("about.title");
        AboutBody.Text = t("about.body");
        AboutLite.Text = t("about.lite");
        AboutOss.Text = t("about.oss");
        AboutRepo.Text = t("about.repo");
        AboutLicense.Text = t("about.license");
        SaveButton.Content = t("btn.save");
        if (string.IsNullOrWhiteSpace(StatusText.Text) || StatusText.Text == t("status.ready") || StatusText.Text == "就绪" || StatusText.Text == "Ready")
        {
            StatusText.Text = t("status.ready");
        }

        var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        VersionText.Text = t("about.version", ver);

        var lang = LanguageBox.SelectedItem is ComboItem li ? li.Value : _engine.Config.Language;
        var cabc = CabcModeBox.SelectedItem is ComboItem ci ? ci.Value : _engine.Config.Display.CabcMode;
        RebuildLanguageBox(lang);
        RebuildCabcBox(cabc);
        UpdateSliderLabels();
    }

    private void RebuildLanguageBox(string selected)
    {
        LanguageBox.Items.Clear();
        LanguageBox.Items.Add(new ComboItem(_engine.I18n.T("general.lang_auto"), "auto"));
        foreach (var lang in _engine.I18n.AvailableLanguages())
        {
            LanguageBox.Items.Add(new ComboItem(lang, lang));
        }

        SelectCombo(LanguageBox, selected);
    }

    private void RebuildCabcBox(string selected)
    {
        CabcModeBox.Items.Clear();
        CabcModeBox.Items.Add(new ComboItem(_engine.I18n.T("display.cabc_system"), "system"));
        CabcModeBox.Items.Add(new ComboItem(_engine.I18n.T("display.cabc_off"), "off"));
        CabcModeBox.Items.Add(new ComboItem(_engine.I18n.T("display.cabc_always"), "always"));
        CabcModeBox.Items.Add(new ComboItem(_engine.I18n.T("display.cabc_battery"), "battery"));
        SelectCombo(CabcModeBox, selected);
    }

    private static void SelectCombo(System.Windows.Controls.ComboBox box, string value)
    {
        for (var i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is ComboItem item && item.Value.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                box.SelectedIndex = i;
                return;
            }
        }

        if (box.Items.Count > 0)
        {
            box.SelectedIndex = 0;
        }
    }

    private static System.Windows.Media.Brush BrushFromHex(string hex)
        => TryParseColor(hex, out var c)
            ? new SolidColorBrush(c)
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6));

    private static bool TryParseColor(string? hex, out System.Windows.Media.Color color)
    {
        color = default;
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

        color = System.Windows.Media.Color.FromArgb(
            (byte)((argb >> 24) & 0xFF),
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF));
        return true;
    }

    private static void TryRelaunchElevated()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe))
            {
                return;
            }

            Process.Start(new ProcessStartInfo(exe)
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch
        {
            // UAC cancelled
        }
    }

    private sealed class ComboItem(string label, string value)
    {
        public string Label { get; } = label;
        public string Value { get; } = value;
        public override string ToString() => Label;
    }
}
