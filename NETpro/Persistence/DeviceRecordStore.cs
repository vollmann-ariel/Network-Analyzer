using System.IO;
using System.Text.Json;

namespace NETpro.Persistence;

public sealed record DeviceRecord(string Label, string? LastKnownIp, string? LastKnownVendor);

public interface IDeviceRecordStore
{
    DeviceRecord? GetRecord(string key);
    void SetLabel(string key, string label);
    void SetLastSeen(string key, string ip, string? vendor);
    void SetVendor(string key, string vendor);
    IReadOnlyDictionary<string, DeviceRecord> GetAllRecords();
}

/// <summary>
/// One record per device, keyed by MAC address (stable across DHCP-driven IP changes; the
/// self row has no MAC, so callers use the constant key "self" for it instead). Holds both the
/// user-assigned label and the last IP/vendor seen for the device, so a device that's currently
/// unreachable can still be listed and identified instead of only showing up mid-scan.
/// </summary>
public sealed class JsonFileDeviceRecordStore : IDeviceRecordStore
{
    public const string SelfKey = "self";

    private readonly string _path;
    private readonly Dictionary<string, DeviceRecord> _records;

    public JsonFileDeviceRecordStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NETpro", "devices.json");
        _records = Load(_path);
    }

    public DeviceRecord? GetRecord(string key) => _records.GetValueOrDefault(key);

    public IReadOnlyDictionary<string, DeviceRecord> GetAllRecords() => new Dictionary<string, DeviceRecord>(_records);

    public void SetLabel(string key, string label)
    {
        var current = _records.GetValueOrDefault(key);
        if (string.IsNullOrWhiteSpace(label) && current is { LastKnownIp: null, LastKnownVendor: null })
        {
            _records.Remove(key);
        }
        else
        {
            _records[key] = (current ?? new DeviceRecord("", null, null)) with { Label = label };
        }
        Save();
    }

    public void SetLastSeen(string key, string ip, string? vendor)
    {
        var current = _records.GetValueOrDefault(key) ?? new DeviceRecord("", null, null);
        _records[key] = current with { LastKnownIp = ip, LastKnownVendor = vendor };
        Save();
    }

    public void SetVendor(string key, string vendor)
    {
        var current = _records.GetValueOrDefault(key) ?? new DeviceRecord("", null, null);
        _records[key] = current with { LastKnownVendor = vendor };
        Save();
    }

    private static Dictionary<string, DeviceRecord> Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, DeviceRecord>>(File.ReadAllText(path)) ?? []
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
        File.WriteAllText(_path, JsonSerializer.Serialize(_records));
    }
}
