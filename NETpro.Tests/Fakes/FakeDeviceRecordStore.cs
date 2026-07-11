using NETpro.Persistence;

namespace NETpro.Tests.Fakes;

public sealed class FakeDeviceRecordStore : IDeviceRecordStore
{
    private readonly Dictionary<string, DeviceRecord> _records = new();

    public DeviceRecord? GetRecord(string key) => _records.GetValueOrDefault(key);

    public IReadOnlyDictionary<string, DeviceRecord> GetAllRecords() => new Dictionary<string, DeviceRecord>(_records);

    public void SetLabel(string key, string label) =>
        _records[key] = (_records.GetValueOrDefault(key) ?? new DeviceRecord("", null, null)) with { Label = label };

    public void SetLastSeen(string key, string ip, string? vendor) =>
        _records[key] = (_records.GetValueOrDefault(key) ?? new DeviceRecord("", null, null)) with { LastKnownIp = ip, LastKnownVendor = vendor };

    public void SetVendor(string key, string vendor) =>
        _records[key] = (_records.GetValueOrDefault(key) ?? new DeviceRecord("", null, null)) with { LastKnownVendor = vendor };

    public void Remove(string key) => _records.Remove(key);
}
