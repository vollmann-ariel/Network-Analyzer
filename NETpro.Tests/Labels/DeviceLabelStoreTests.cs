using NETpro.Labels;

namespace NETpro.Tests.Labels;

public class DeviceLabelStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"netpro-labels-{Guid.NewGuid()}.json");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public void GetLabel_ReturnsNull_ForUnknownKey()
    {
        var store = new JsonFileDeviceLabelStore(_path);
        Assert.Null(store.GetLabel("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public void GetLabel_ReturnsSetValue_AfterSetLabel()
    {
        var store = new JsonFileDeviceLabelStore(_path);
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora oficina");
        Assert.Equal("Impresora oficina", store.GetLabel("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public void GetLabel_PersistsAcrossInstances_ThroughTheBackingFile()
    {
        new JsonFileDeviceLabelStore(_path).SetLabel("aa:bb:cc:dd:ee:ff", "Impresora oficina");
        var reloaded = new JsonFileDeviceLabelStore(_path);
        Assert.Equal("Impresora oficina", reloaded.GetLabel("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public void SetLabel_WithEmptyString_RemovesTheEntry()
    {
        var store = new JsonFileDeviceLabelStore(_path);
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora oficina");
        store.SetLabel("aa:bb:cc:dd:ee:ff", "");
        Assert.Null(store.GetLabel("aa:bb:cc:dd:ee:ff"));
    }
}
