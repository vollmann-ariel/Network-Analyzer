using System.IO;
using System.Text.Json;

namespace NETpro.Persistence;

public sealed record AppSettings(
    bool AutoRefreshEnabled,
    int AutoRefreshIntervalSeconds,
    double WindowWidth = 800,
    double WindowHeight = 500,
    IReadOnlyDictionary<string, double>? ColumnWidths = null)
{
    public IReadOnlyDictionary<string, double> ColumnWidths { get; init; } = ColumnWidths ?? new Dictionary<string, double>();

    public static readonly AppSettings Default = new(AutoRefreshEnabled: false, AutoRefreshIntervalSeconds: 30);

    // IReadOnlyDictionary has no structural equality of its own, so the compiler-synthesized
    // record Equals (which would compare it by reference) needs overriding — otherwise two
    // AppSettings with identical column widths in different dictionary instances (e.g. one
    // loaded fresh from JSON) would compare as unequal.
    public bool Equals(AppSettings? other) =>
        other is not null
        && AutoRefreshEnabled == other.AutoRefreshEnabled
        && AutoRefreshIntervalSeconds == other.AutoRefreshIntervalSeconds
        && WindowWidth.Equals(other.WindowWidth)
        && WindowHeight.Equals(other.WindowHeight)
        && ColumnWidths.Count == other.ColumnWidths.Count
        && ColumnWidths.All(kv => other.ColumnWidths.TryGetValue(kv.Key, out var value) && value.Equals(kv.Value));

    public override int GetHashCode() =>
        HashCode.Combine(AutoRefreshEnabled, AutoRefreshIntervalSeconds, WindowWidth, WindowHeight, ColumnWidths.Count);
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
