using CommunityToolkit.Mvvm.ComponentModel;

namespace NETpro.Models;

public partial class DeviceInfo(string ipAddress, string? macAddress, string? vendor, bool isSelf) : ObservableObject
{
    public string IpAddress { get; } = ipAddress;
    public string? MacAddress { get; } = macAddress;
    public string? Vendor { get; } = vendor;
    public bool IsSelf { get; } = isSelf;

    [ObservableProperty]
    private long? _pingTimeMs;

    public string PingDisplay => PingTimeMs is { } ms ? $"{ms} ms" : "Sin respuesta";

    [ObservableProperty]
    private string _label = string.Empty;

    partial void OnPingTimeMsChanged(long? value) => OnPropertyChanged(nameof(PingDisplay));
}
