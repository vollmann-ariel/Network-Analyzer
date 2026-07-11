using System.IO;
using System.Text.Json;

namespace NETpro.Labels;

public interface IDeviceLabelStore
{
    string? GetLabel(string key);
    void SetLabel(string key, string label);
}

/// <summary>
/// Labels are keyed by MAC address (stable across DHCP-driven IP changes) rather than IP.
/// The self row has no MAC, so callers use the constant key "self" for it instead.
/// </summary>
public sealed class JsonFileDeviceLabelStore : IDeviceLabelStore
{
    public const string SelfKey = "self";

    private readonly string _path;
    private readonly Dictionary<string, string> _labels;

    public JsonFileDeviceLabelStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NETpro", "labels.json");
        _labels = Load(_path);
    }

    public string? GetLabel(string key) => _labels.GetValueOrDefault(key);

    public void SetLabel(string key, string label)
    {
        if (string.IsNullOrWhiteSpace(label)) _labels.Remove(key);
        else _labels[key] = label;
        Save();
    }

    private static Dictionary<string, string> Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? []
                : [];
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return [];
        }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_path, JsonSerializer.Serialize(_labels));
    }
}
