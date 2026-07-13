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
    /// Every labeled device — not just ones currently online — should be visible immediately
    /// on startup, even before a scan confirms it's reachable. Otherwise a known device that's
    /// temporarily off the network would disappear from the list entirely and we'd have to
    /// rediscover it (and re-resolve its vendor) from scratch on every launch. Unlabeled
    /// records have no such purpose to the user, so they're purged here instead of piling up
    /// forever in the store.
    /// </summary>
    private void SeedKnownDevices()
    {
        foreach (var (key, record) in _recordStore.GetAllRecords())
        {
            if (key == JsonFileDeviceRecordStore.SelfKey) continue;
            if (string.IsNullOrEmpty(record.Label))
            {
                _recordStore.Remove(key);
                continue;
            }
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

        // Only labeled devices are worth remembering once they go offline — an unlabeled one
        // (including a randomized MAC, which rotates per network and is never seen again under
        // this address anyway) would otherwise just accumulate stale entries in the store.
        if (!scanned.IsSelf && ShouldPersist(tracked))
        {
            _recordStore.SetLastSeen(key, scanned.IpAddress, scanned.Vendor);
        }

        // HasScanned is still false during the app's very first scan (it flips only after
        // RefreshAsync finishes), so a cold start with an empty device history doesn't fire
        // one notification per device already sitting on the LAN. This is deliberately not
        // gated on ShouldPersist: a brand-new device is unlabeled by definition, so that check
        // would suppress every single notification.
        if (isNewDevice && !scanned.IsSelf && HasScanned && IsWorthNotifying(tracked))
        {
            _deviceNotifier.NotifyNewDevice(tracked);
        }
    }

    private static bool ShouldPersist(DeviceInfo device) => !string.IsNullOrEmpty(device.Label);

    private static bool IsWorthNotifying(DeviceInfo device) => device.Vendor != TsvOuiVendorLookup.RandomizedMacVendor;

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
            // Clearing the label must drop the record entirely (same ShouldPersist rule
            // MergeDevice uses) — otherwise the device keeps loading as a nameless offline
            // entry on every launch instead of actually being forgotten.
            if (ShouldPersist(device)) _recordStore.SetLabel(key, device.Label);
            else _recordStore.Remove(key);
        };
    }

    private static string LabelKeyFor(DeviceInfo device) =>
        device.IsSelf ? JsonFileDeviceRecordStore.SelfKey : device.MacAddress ?? device.IpAddress;
}
