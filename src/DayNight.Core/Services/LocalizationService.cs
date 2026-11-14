using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace DayNight.Core.Services;

public sealed class LocalizationService
{
    private readonly Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _localesDirectory;

    public string CurrentLanguage { get; private set; } = "en";

    public event EventHandler? LanguageChanged;

    public LocalizationService(string? localesDirectory = null)
    {
        _localesDirectory = localesDirectory
            ?? Path.Combine(AppContext.BaseDirectory, "locales");
    }

    public IReadOnlyList<string> AvailableLanguages()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "en", "zh-CN" };
        foreach (var name in EnumerateEmbeddedLocaleNames())
        {
            set.Add(name);
        }

        if (Directory.Exists(_localesDirectory))
        {
            foreach (var path in Directory.GetFiles(_localesDirectory, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    set.Add(name);
                }
            }
        }

        return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public void ApplyLanguagePreference(string? preference)
    {
        var lang = ResolveLanguage(preference);
        Load(lang);
    }

    public string ResolveLanguage(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference) || preference.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            var ui = CultureInfo.CurrentUICulture.Name;
            if (ui.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return AvailableLanguages().Contains("zh-CN", StringComparer.OrdinalIgnoreCase)
                    ? "zh-CN"
                    : "en";
            }

            return "en";
        }

        return preference;
    }

    public void Load(string language)
    {
        _strings.Clear();
        CurrentLanguage = language;

        foreach (var json in CandidateJson(language))
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        _strings[prop.Name] = prop.Value.GetString() ?? prop.Name;
                    }
                }

                LanguageChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
            catch
            {
                // try next
            }
        }

        SeedEnglishFallback();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string T(string key, params object[] args)
    {
        if (!_strings.TryGetValue(key, out var template))
        {
            template = key;
        }

        if (args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    private IEnumerable<string?> CandidateJson(string language)
    {
        foreach (var name in ExpandLanguageNames(language))
        {
            yield return TryReadEmbedded(name);
            var path = Path.Combine(_localesDirectory, $"{name}.json");
            if (File.Exists(path))
            {
                yield return File.ReadAllText(path);
            }
        }
    }

    private static IEnumerable<string> ExpandLanguageNames(string language)
    {
        yield return language;
        if (language.Contains('-', StringComparison.Ordinal))
        {
            yield return language.Split('-')[0];
        }

        if (!language.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            yield return "en";
        }
    }

    private static string? TryReadEmbedded(string language)
    {
        var asm = typeof(LocalizationService).Assembly;
        var logical = $"DayNight.Core.locales.{language}.json";
        using var stream = asm.GetManifestResourceStream(logical);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static IEnumerable<string> EnumerateEmbeddedLocaleNames()
    {
        var asm = typeof(LocalizationService).Assembly;
        const string prefix = "DayNight.Core.locales.";
        const string suffix = ".json";
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                !name.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            yield return name[prefix.Length..^suffix.Length];
        }
    }

    private void SeedEnglishFallback()
    {
        CurrentLanguage = "en";
        _strings["app.name"] = "DayNight";
        _strings["tray.open"] = "Open DayNight";
        _strings["tray.exit"] = "Exit";
    }
}
