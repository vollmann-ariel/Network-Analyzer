using NETpro.Persistence;

namespace NETpro.Tests.Fakes;

public sealed class FakeAppSettingsStore(AppSettings? initial = null) : IAppSettingsStore
{
    private AppSettings _settings = initial ?? AppSettings.Default;

    public AppSettings Load() => _settings;

    public void Save(AppSettings settings) => _settings = settings;
}
