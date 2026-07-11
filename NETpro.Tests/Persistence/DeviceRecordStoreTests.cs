using NETpro.Persistence;

namespace NETpro.Tests.Persistence;

public class DeviceRecordStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"netpro-devices-{Guid.NewGuid()}.json");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public void GetRecord_ReturnsNull_ForUnknownKey()
    {
        var store = new JsonFileDeviceRecordStore(_path);
        Assert.Null(store.GetRecord("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public void GetRecord_ReturnsSetLabel_AfterSetLabel()
    {
        var store = new JsonFileDeviceRecordStore(_path);
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora oficina");
        Assert.Equal("Impresora oficina", store.GetRecord("aa:bb:cc:dd:ee:ff")?.Label);
    }

    [Fact]
    public void GetRecord_PersistsAcrossInstances_ThroughTheBackingFile()
    {
        new JsonFileDeviceRecordStore(_path).SetLabel("aa:bb:cc:dd:ee:ff", "Impresora oficina");
        var reloaded = new JsonFileDeviceRecordStore(_path);
        Assert.Equal("Impresora oficina", reloaded.GetRecord("aa:bb:cc:dd:ee:ff")?.Label);
    }

    [Fact]
    public void SetLabel_WithEmptyString_RemovesTheEntry_WhenThereIsNoSightingData()
    {
        var store = new JsonFileDeviceRecordStore(_path);
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora oficina");
        store.SetLabel("aa:bb:cc:dd:ee:ff", "");
        Assert.Null(store.GetRecord("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public void SetLabel_WithEmptyString_KeepsTheRecord_WhenSightingDataExists()
    {
        var store = new JsonFileDeviceRecordStore(_path);
        store.SetLastSeen("aa:bb:cc:dd:ee:ff", "192.168.1.20", "Acme Inc.");
        store.SetLabel("aa:bb:cc:dd:ee:ff", "");
        Assert.Equal("192.168.1.20", store.GetRecord("aa:bb:cc:dd:ee:ff")?.LastKnownIp);
    }

    [Fact]
    public void SetLastSeen_RecordsIpAndVendor_ForADeviceWithNoLabel()
    {
        var store = new JsonFileDeviceRecordStore(_path);
        store.SetLastSeen("aa:bb:cc:dd:ee:ff", "192.168.1.20", "Acme Inc.");
        Assert.Equal("Acme Inc.", store.GetRecord("aa:bb:cc:dd:ee:ff")?.LastKnownVendor);
    }

    [Fact]
    public void SetVendor_FillsInTheVendor_ForADeviceThatOnlyHadALabel()
    {
        var store = new JsonFileDeviceRecordStore(_path);
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora oficina");
        store.SetVendor("aa:bb:cc:dd:ee:ff", "Acme Inc.");
        Assert.Equal("Acme Inc.", store.GetRecord("aa:bb:cc:dd:ee:ff")?.LastKnownVendor);
    }

    [Fact]
    public void SetVendor_DoesNotTouchTheExistingLabel()
    {
        var store = new JsonFileDeviceRecordStore(_path);
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora oficina");
        store.SetVendor("aa:bb:cc:dd:ee:ff", "Acme Inc.");
        Assert.Equal("Impresora oficina", store.GetRecord("aa:bb:cc:dd:ee:ff")?.Label);
    }

    [Fact]
    public void GetAllRecords_IncludesEverySetEntry()
    {
        var store = new JsonFileDeviceRecordStore(_path);
        store.SetLastSeen("aa:bb:cc:dd:ee:ff", "192.168.1.20", "Acme Inc.");
        Assert.Contains("aa:bb:cc:dd:ee:ff", store.GetAllRecords().Keys);
    }
}
