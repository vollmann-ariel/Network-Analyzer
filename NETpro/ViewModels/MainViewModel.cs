using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NETpro.Models;
using NETpro.Networking;
using NETpro.Notifications;
using NETpro.Oui;
using NETpro.Persistence;
using Timer = System.Threading.Timer;

namespace NETpro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string UnknownIpPlaceholder = "—";

    private readonly Func<Task<NetworkScanner>> _scannerProvider;
    private readonly IDeviceRecordStore _recordStore;
    private readonly IAppSettingsStore _settingsStore;
    private readonly IDeviceNotifier _deviceNotifier;
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;
    private Timer? _autoRefreshTimer;

    public MainViewModel(
        Func<Task<NetworkScanner>> scannerProvider,
        IDeviceRecordStore recordStore,
        IAppSettingsStore settingsStore,
        IDeviceNotifier deviceNotifier)
    {
        _scannerProvider = scannerProvider;
        _recordStore = recordStore;
        _settingsStore = settingsStore;
        _deviceNotifier = deviceNotifier;
        SeedKnownDevices();

        var settings = _settingsStore.Load();
        AutoRefreshIntervalSeconds = settings.AutoRefreshIntervalSeconds;
        AutoRefreshEnabled = settings.AutoRefreshEnabled;
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
        SaveAutoRefreshSettings();
    }

    partial void OnAutoRefreshIntervalSecondsChanged(int value)
    {
        if (AutoRefreshEnabled) RestartAutoRefreshTimer();
        SaveAutoRefreshSettings();
    }

    private void SaveAutoRefreshSettings() =>
        _settingsStore.Save(_settingsStore.Load() with
        {
            AutoRefreshEnabled = AutoRefreshEnabled,
            AutoRefreshIntervalSeconds = AutoRefreshIntervalSeconds
        });

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

    /// <summary>
    /// Every device ever detected — not just ones the user has labeled — should be visible
    /// immediately on startup, even before a scan confirms it's currently reachable. Otherwise
    /// a known device that's temporarily off the network would disappear from the list entirely
    /// and we'd have to rediscover it (and re-resolve its vendor) from scratch on every launch.
    /// </summary>
    private void SeedKnownDevices()
    {
        foreach (var (key, record) in _recordStore.GetAllRecords())
        {
            if (key == JsonFileDeviceRecordStore.SelfKey) continue;
            var device = new DeviceInfo(record.LastKnownIp ?? UnknownIpPlaceholder, macAddress: key, vendor: record.LastKnownVendor, isSelf: false)
            {
                Label = record.Label
            };
            SubscribeLabelPersistence(device, key);
            Devices.Add(device);
        }
    }

    /// <summary>
    /// Vendor is a pure function of the MAC address — it doesn't require the device to actually
    /// respond. Devices seeded from a record with no cached vendor (e.g. carried over from a
    /// label with no scan history yet) would otherwise show a blank Fabricante forever, since
    /// MergeDevice only touches devices an actual scan finds. Called once the OUI table has
    /// finished loading; the result is cached back to the store so this only runs once per device.
    /// </summary>
    public void ApplyVendorLookup(IOuiVendorLookup vendorLookup)
    {
        foreach (var device in Devices.Where(d => d.Vendor is null && d.MacAddress is not null))
        {
            var vendor = vendorLookup.Lookup(device.MacAddress!);
            device.Vendor = vendor;
            _recordStore.SetVendor(LabelKeyFor(device), vendor);
        }
    }

    private void MergeDevice(DeviceInfo scanned)
    {
        var key = LabelKeyFor(scanned);
        var existing = Devices.FirstOrDefault(d => LabelKeyFor(d) == key);
        var isNewDevice = existing is null;
        DeviceInfo tracked;
        if (existing is null)
        {
            AttachPersistedLabel(scanned);
            if (scanned.IsSelf) Devices.Insert(0, scanned);
            else Devices.Add(scanned);
            tracked = scanned;
        }
        else
        {
            existing.IpAddress = scanned.IpAddress;
            existing.Vendor = scanned.Vendor;
            existing.PingTimeMs = scanned.PingTimeMs;
            existing.OpenPorts = scanned.OpenPorts;
            tracked = existing;
        }

        // Randomized MACs rotate per network the device joins, so an unlabeled one is never
        // going to be seen again under this address — persisting it would just accumulate
        // stale entries in the store forever. Once the user names it, it's worth keeping.
        if (!scanned.IsSelf && ShouldPersist(tracked))
        {
            _recordStore.SetLastSeen(key, scanned.IpAddress, scanned.Vendor);
        }

        // HasScanned is still false during the app's very first scan (it flips only after
        // RefreshAsync finishes), so a cold start with an empty device history doesn't fire
        // one notification per device already sitting on the LAN.
        if (isNewDevice && !scanned.IsSelf && HasScanned && ShouldPersist(tracked))
        {
            _deviceNotifier.NotifyNewDevice(tracked);
        }
    }

    private static bool ShouldPersist(DeviceInfo device) =>
        !string.IsNullOrEmpty(device.Label) || device.Vendor != TsvOuiVendorLookup.RandomizedMacVendor;

    private void AttachPersistedLabel(DeviceInfo device)
    {
        var key = LabelKeyFor(device);
        var storedLabel = _recordStore.GetRecord(key)?.Label;
        device.Label = !string.IsNullOrEmpty(storedLabel) ? storedLabel : (device.IsSelf ? "Este equipo" : string.Empty);
        SubscribeLabelPersistence(device, key);
    }

    private void SubscribeLabelPersistence(DeviceInfo device, string key)
    {
        device.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(DeviceInfo.Label)) return;
            // Clearing the label on an unlabeled-randomized-MAC device must drop the record
            // entirely (same ShouldPersist rule MergeDevice uses), or it re-accumulates the
            // very "flooding" entries the label check was added to prevent.
            if (ShouldPersist(device)) _recordStore.SetLabel(key, device.Label);
            else _recordStore.Remove(key);
        };
    }

    private static string LabelKeyFor(DeviceInfo device) =>
        device.IsSelf ? JsonFileDeviceRecordStore.SelfKey : device.MacAddress ?? device.IpAddress;
}
