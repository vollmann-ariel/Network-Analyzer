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
}
