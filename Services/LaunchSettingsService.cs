using System.IO;
using System.Text.Json;
using DragonGlareAlpha.Domain.Startup;

namespace DragonGlareAlpha.Services;

public sealed class LaunchSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DragonGlareAlpha",
        "launch_settings.json");

    public LaunchSettings Load()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return new LaunchSettings();
            }

            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<LaunchSettings>(json, SerializerOptions);
            return settings ?? new LaunchSettings();
        }
        catch
        {
            return new LaunchSettings();
        }
    }

    public void Save(LaunchSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(settingsPath, json);
        }
        catch
        {
        }
    }
}
