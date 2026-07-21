using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstaDM.Core.Settings;

/// <summary>
/// Local, non-roaming JSON settings store. The host supplies a directory under
/// %LOCALAPPDATA%\InstaDM (or a test temp folder). Atomic write via temp+replace.
/// Never stores secrets, cookies, messages, or URLs with identifiers.
/// </summary>
public sealed class LocalSettingsStore
{
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _filePath;
    private readonly object _gate = new();

    public LocalSettingsStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, FileName);
    }

    public string FilePath => _filePath;

    public AppSettings Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_filePath))
            {
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                    ?? new AppSettings();
                settings.Normalize();
                return settings;
            }
            catch (JsonException)
            {
                return new AppSettings();
            }
            catch (IOException)
            {
                return new AppSettings();
            }
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();

        lock (_gate)
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, _filePath, overwrite: true);
        }
    }

    /// <summary>Deletes the settings file so the next Load returns defaults.
    /// Does not touch the WebView profile.</summary>
    public void ResetToDefaults()
    {
        lock (_gate)
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }
}
