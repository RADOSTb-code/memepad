using System.Text.Json;
using System.Text.Json.Serialization;
using MemePad.Core.Model;

namespace MemePad.Core.Settings;

/// <summary>Loads and saves <see cref="AppSettings"/> as JSON in one directory, never leaving a half-written file.</summary>
public sealed class SettingsStore(string directory)
{
    private const string FileName = "settings.json";

    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private string FilePath => Path.Combine(directory, FileName);

    public AppSettings Load()
    {
        if (!File.Exists(FilePath)) return new AppSettings();

        try
        {
            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Json);
            if (loaded is null) return Recover();
            if (loaded.Pages.Count == 0) loaded.Pages.Add(AppSettings.NewPage(AppSettings.DefaultPageName));
            loaded.ActivePage = Math.Clamp(loaded.ActivePage, 0, loaded.Pages.Count - 1);
            return loaded;
        }
        catch (JsonException)
        {
            return Recover();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(directory);
        var tmp = FilePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(settings, Json));
        if (File.Exists(FilePath)) File.Replace(tmp, FilePath, destinationBackupFileName: null);
        else File.Move(tmp, FilePath);
    }

    private AppSettings Recover()
    {
        File.Copy(FilePath, FilePath + ".bad", overwrite: true);
        return new AppSettings();
    }
}
