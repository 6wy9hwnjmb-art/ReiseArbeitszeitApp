using System.IO;
using System.Text.Json;
using ReiseArbeitszeitApp.Models;

namespace ReiseArbeitszeitApp.Services;

public class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReiseArbeitszeitApp");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public string SettingsPath => _settingsPath;

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }
}
