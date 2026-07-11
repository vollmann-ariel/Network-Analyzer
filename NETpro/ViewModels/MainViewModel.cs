using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NETpro.Labels;
using NETpro.Models;
using NETpro.Networking;
using Timer = System.Threading.Timer;

namespace NETpro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Func<Task<NetworkScanner>> _scannerProvider;
    private readonly IDeviceLabelStore _labelStore;
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;
    private Timer? _autoRefreshTimer;

    public MainViewModel(Func<Task<NetworkScanner>> scannerProvider, IDeviceLabelStore labelStore)
    {
        _scannerProvider = scannerProvider;
        _labelStore = labelStore;
    }

    public ObservableCollection<DeviceInfo> Devices { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasScanned;

    [ObservableProperty]
    private bool _showEmptyHint;

    [ObservableProperty]
    private bool _autoRefreshEnabled;

    [ObservableProperty]
    private int _autoRefreshIntervalSeconds = 30;

    partial void OnAutoRefreshEnabledChanged(bool value)
    {
        if (value) RestartAutoRefreshTimer();
        else StopAutoRefreshTimer();
    }

    partial void OnAutoRefreshIntervalSecondsChanged(int value)
    {
        if (AutoRefreshEnabled) RestartAutoRefreshTimer();
    }

    private void RestartAutoRefreshTimer()
    {
        StopAutoRefreshTimer();
        var interval = TimeSpan.FromSeconds(Math.Max(1, AutoRefreshIntervalSeconds));
        _autoRefreshTimer = new Timer(_ => TriggerAutoRefresh(), null, interval, interval);
    }

    private void StopAutoRefreshTimer()
    {
        _autoRefreshTimer?.Dispose();
        _autoRefreshTimer = null;
    }

    private void TriggerAutoRefresh()
    {
        if (_uiContext is not null) _uiContext.Post(_ => RefreshCommand.Execute(null), null);
        else RefreshCommand.Execute(null);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var scanner = await _scannerProvider();
            switch (await scanner.ScanAsync())
            {
                case ScanResult.Success success:
                    foreach (var device in success.Devices) MergeDevice(device);
                    ShowEmptyHint = Devices.Count == 1;
                    break;
                case ScanResult.NoActiveNetwork:
                    ErrorMessage = "No se detectó una red activa. Conectate a tu red e intentá de nuevo.";
                    break;
                case ScanResult.SubnetTooLarge:
                    ErrorMessage = $"Esta red es demasiado grande para escanear (más de {NetworkScanner.MaxScanHosts} direcciones posibles).";
                    break;
                case ScanResult.ArpUnavailable:
                    ErrorMessage = "No se pudo leer la tabla de vecinos IP en este equipo.";
                    break;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Ocurrió un error inesperado al escanear la red.";
        }
        finally
        {
            IsLoading = false;
            HasScanned = true;
        }
    }

    private void MergeDevice(DeviceInfo scanned)
    {
        var key = LabelKeyFor(scanned);
        var existing = Devices.FirstOrDefault(d => LabelKeyFor(d) == key);
        if (existing is null)
        {
            AttachPersistedLabel(scanned);
            Devices.Add(scanned);
        }
        else
        {
            existing.PingTimeMs = scanned.PingTimeMs;
        }
    }

    private void AttachPersistedLabel(DeviceInfo device)
    {
        var key = LabelKeyFor(device);
        device.Label = _labelStore.GetLabel(key) ?? (device.IsSelf ? "Este equipo" : string.Empty);
        device.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DeviceInfo.Label)) _labelStore.SetLabel(key, device.Label);
        };
    }

    private static string LabelKeyFor(DeviceInfo device) =>
        device.IsSelf ? JsonFileDeviceLabelStore.SelfKey : device.MacAddress ?? device.IpAddress;
}
