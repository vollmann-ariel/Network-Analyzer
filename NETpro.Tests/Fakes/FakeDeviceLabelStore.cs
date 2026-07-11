using NETpro.Labels;

namespace NETpro.Tests.Fakes;

public sealed class FakeDeviceLabelStore : IDeviceLabelStore
{
    private readonly Dictionary<string, string> _labels = new();

    public string? GetLabel(string key) => _labels.GetValueOrDefault(key);

    public void SetLabel(string key, string label) => _labels[key] = label;
}
