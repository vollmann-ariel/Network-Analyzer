using System.IO;
using System.Text.Json;

namespace NETpro.Persistence;

public sealed record AppSettings(bool AutoRefreshEnabled, int AutoRefreshIntervalSeconds)
{
    public static readonly AppSettings Default = new(AutoRefreshEnabled: false, AutoRefreshIntervalSeconds: 30);
}

public interface IAppSettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}

public sealed class JsonFileAppSettingsStore(string? path = null) : IAppSettingsStore
{
    private readonly string _path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NETpro", "settings.json");

    public AppSettings Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? AppSettings.Default
                : AppSettings.Default;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings));
    }
}
