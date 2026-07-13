using NETpro.Persistence;

namespace NETpro.Tests.Persistence;

public class AppSettingsStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"netpro-settings-{Guid.NewGuid()}.json");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenNoFileExistsYet()
    {
        var store = new JsonFileAppSettingsStore(_path);
        Assert.Equal(AppSettings.Default, store.Load());
    }

    [Fact]
    public void Load_ReturnsSavedSettings_AfterSave()
    {
        var store = new JsonFileAppSettingsStore(_path);
        store.Save(new AppSettings(AutoRefreshEnabled: true, AutoRefreshIntervalSeconds: 45));
        Assert.Equal(new AppSettings(true, 45), store.Load());
    }

    [Fact]
    public void Load_PersistsAcrossInstances_ThroughTheBackingFile()
    {
        new JsonFileAppSettingsStore(_path).Save(new AppSettings(true, 45));
        var reloaded = new JsonFileAppSettingsStore(_path);
        Assert.Equal(new AppSettings(true, 45), reloaded.Load());
    }

    [Fact]
    public void Load_ReturnsSavedWindowSize_AfterSave()
    {
        var store = new JsonFileAppSettingsStore(_path);
        store.Save(AppSettings.Default with { WindowWidth = 1024, WindowHeight = 700 });
        var loaded = store.Load();
        Assert.Equal((1024, 700), (loaded.WindowWidth, loaded.WindowHeight));
    }

    [Fact]
    public void Load_ReturnsSavedColumnWidths_AfterSave()
    {
        var store = new JsonFileAppSettingsStore(_path);
        store.Save(AppSettings.Default with { ColumnWidths = new Dictionary<string, double> { ["IP"] = 150 } });
        Assert.Equal(150, store.Load().ColumnWidths["IP"]);
    }

    [Fact]
    public void AppSettings_AreEqual_WhenColumnWidthsHaveTheSameContentInDifferentInstances()
    {
        var a = AppSettings.Default with { ColumnWidths = new Dictionary<string, double> { ["IP"] = 100 } };
        var b = AppSettings.Default with { ColumnWidths = new Dictionary<string, double> { ["IP"] = 100 } };
        Assert.Equal(a, b);
    }
}
