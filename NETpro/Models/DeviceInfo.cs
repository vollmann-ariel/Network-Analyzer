using CommunityToolkit.Mvvm.ComponentModel;

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

    partial void OnPingTimeMsChanged(long? value) => OnPropertyChanged(nameof(PingDisplay));
}
