using CommunityToolkit.Mvvm.ComponentModel;
using NETpro.Networking;

namespace NETpro.Models;

public partial class DeviceInfo(string ipAddress, string? macAddress, string? vendor, bool isSelf) : ObservableObject
{
    [ObservableProperty]
    private string _ipAddress = ipAddress;

    public string? MacAddress { get; } = macAddress;

    [ObservableProperty]
    private string? _vendor = vendor;

    public bool IsSelf { get; } = isSelf;

    [ObservableProperty]
    private long? _pingTimeMs;

    public string PingDisplay => PingTimeMs is { } ms ? $"{ms} ms" : "🚫 Sin conexión";

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<int> _openPorts = [];

    public string ServicesDisplay => string.Join(", ",
        OpenPorts.Select(p => WellKnownPorts.Names.GetValueOrDefault(p, p.ToString())).Distinct());

    public string OpenPortsDisplay => string.Join(", ", OpenPorts);

    partial void OnPingTimeMsChanged(long? value) => OnPropertyChanged(nameof(PingDisplay));

    partial void OnOpenPortsChanged(IReadOnlyList<int> value)
    {
        OnPropertyChanged(nameof(ServicesDisplay));
        OnPropertyChanged(nameof(OpenPortsDisplay));
    }
}
