using System.Net;
using NETpro.Networking;
using NETpro.Notifications;
using NETpro.Oui;
using NETpro.Persistence;
using NETpro.Tests.Fakes;
using NETpro.ViewModels;

namespace NETpro.Tests.ViewModels;

public class MainViewModelTests
{
    private static readonly LocalNetworkInfo Info = new(IPAddress.Parse("192.168.1.10"), 24, "Ethernet", 5);

    private static Func<Task<NetworkScanner>> ScannerProvider(
        LocalNetworkInfo? info,
        IArpTableReader? arpTableReader = null,
        IOuiVendorLookup? vendorLookup = null) =>
        () => Task.FromResult(new NetworkScanner(
            new FakeNetworkInfoProvider(info),
            new NetworkSweeper(new NoOpHostProber()),
            arpTableReader ?? new FakeArpTableReader([]),
            vendorLookup ?? new FakeOuiVendorLookup(),
            new FakePingTimeMeasurer(),
            new FakePortScanner()));

    private static MainViewModel BuildViewModel(
        Func<Task<NetworkScanner>> scannerProvider,
        IDeviceRecordStore? recordStore = null,
        IAppSettingsStore? settingsStore = null,
        IDeviceNotifier? deviceNotifier = null) =>
        new(scannerProvider, recordStore ?? new FakeDeviceRecordStore(), settingsStore ?? new FakeAppSettingsStore(), deviceNotifier ?? new FakeDeviceNotifier());

    [Fact]
    public async Task RefreshCommand_PopulatesDevices_OnSuccess()
    {
        var vm = BuildViewModel(ScannerProvider(Info));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Single(vm.Devices);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenNoActiveNetwork()
    {
        var vm = BuildViewModel(ScannerProvider(null));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenSubnetTooLarge()
    {
        var largeInfo = new LocalNetworkInfo(IPAddress.Parse("192.168.1.10"), 20, "Ethernet", 5);
        var vm = BuildViewModel(ScannerProvider(largeInfo));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenArpUnavailable()
    {
        var vm = BuildViewModel(ScannerProvider(Info, new ThrowingArpTableReader()));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsIsLoadingFalse_AfterCompletion()
    {
        var vm = BuildViewModel(ScannerProvider(Info));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task RefreshCommand_SetsHasScannedTrue_AfterCompletion()
    {
        var vm = BuildViewModel(ScannerProvider(Info));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.True(vm.HasScanned);
    }

    [Fact]
    public async Task RefreshCommand_DefaultsSelfLabel_ToEsteEquipo()
    {
        var vm = BuildViewModel(ScannerProvider(Info));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("Este equipo", vm.Devices.Single(d => d.IsSelf).Label);
    }

    [Fact]
    public async Task RefreshCommand_RestoresPersistedLabel_OnNextScan()
    {
        var store = new FakeDeviceRecordStore();
        var vm = BuildViewModel(ScannerProvider(Info), store);
        await vm.RefreshCommand.ExecuteAsync(null);
        vm.Devices.Single(d => d.IsSelf).Label = "Mi PC";

        var vm2 = BuildViewModel(ScannerProvider(Info), store);
        await vm2.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("Mi PC", vm2.Devices.Single(d => d.IsSelf).Label);
    }

    [Fact]
    public async Task RefreshCommand_KeepsPreviouslySeenDevice_WhenALaterScanDoesNotFindIt()
    {
        var scanCount = 0;
        Task<NetworkScanner> Provider()
        {
            scanCount++;
            IReadOnlyList<NeighborEntry> entries = scanCount == 1
                ? [new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true)]
                : [];
            return Task.FromResult(new NetworkScanner(
                new FakeNetworkInfoProvider(Info),
                new NetworkSweeper(new NoOpHostProber()),
                new FakeArpTableReader(entries),
                new FakeOuiVendorLookup(),
                new FakePingTimeMeasurer(),
                new FakePortScanner()));
        }

        var vm = BuildViewModel(Provider);
        await vm.RefreshCommand.ExecuteAsync(null);
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Devices.Count);
    }

    [Fact]
    public void Constructor_SeedsADevice_ForEachPersistedLabel()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora");

        var vm = BuildViewModel(ScannerProvider(Info), store);

        Assert.Equal("Impresora", vm.Devices.Single().Label);
    }

    [Fact]
    public void Constructor_DoesNotSeedADevice_ForTheSelfLabelKey()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel(JsonFileDeviceRecordStore.SelfKey, "Mi PC");

        var vm = BuildViewModel(ScannerProvider(Info), store);

        Assert.Empty(vm.Devices);
    }

    [Fact]
    public async Task RefreshCommand_FillsInIpAddress_ForASeededDeviceThatIsThenDetected()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora");
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(ScannerProvider(Info, new FakeArpTableReader(entries)), store);

        await vm.RefreshCommand.ExecuteAsync(null);

        var device = vm.Devices.Single(d => d.MacAddress == "aa:bb:cc:dd:ee:ff");
        Assert.Equal("192.168.1.20", device.IpAddress);
    }

    [Fact]
    public async Task RefreshCommand_DoesNotPersist_ADeviceWithNoLabel()
    {
        var store = new FakeDeviceRecordStore();
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(ScannerProvider(Info, new FakeArpTableReader(entries)), store);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Null(store.GetRecord("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public async Task RefreshCommand_PersistsLastSeenIp_ForALabeledDevice()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora");
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(ScannerProvider(Info, new FakeArpTableReader(entries)), store);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("192.168.1.20", store.GetRecord("aa:bb:cc:dd:ee:ff")?.LastKnownIp);
    }

    [Fact]
    public async Task RefreshCommand_DoesNotPersist_ARandomizedMacDeviceWithNoLabel()
    {
        var store = new FakeDeviceRecordStore();
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(
            ScannerProvider(Info, new FakeArpTableReader(entries), new FakeOuiVendorLookup(TsvOuiVendorLookup.RandomizedMacVendor)),
            store);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Null(store.GetRecord("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public async Task RefreshCommand_PersistsARandomizedMacDevice_WhenTheUserHasLabeledIt()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Celular Ariel");
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(
            ScannerProvider(Info, new FakeArpTableReader(entries), new FakeOuiVendorLookup(TsvOuiVendorLookup.RandomizedMacVendor)),
            store);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("192.168.1.20", store.GetRecord("aa:bb:cc:dd:ee:ff")?.LastKnownIp);
    }

    [Fact]
    public async Task RefreshCommand_DoesNotNotify_ForANewDevice_OnTheFirstScanOfTheSession()
    {
        var notifier = new FakeDeviceNotifier();
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(ScannerProvider(Info, new FakeArpTableReader(entries)), deviceNotifier: notifier);

        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Empty(notifier.NotifiedDevices);
    }

    [Fact]
    public async Task RefreshCommand_NotifiesNewDevice_OnALaterScan()
    {
        var notifier = new FakeDeviceNotifier();
        var scanCount = 0;
        Task<NetworkScanner> Provider()
        {
            scanCount++;
            IReadOnlyList<NeighborEntry> entries = scanCount == 1
                ? []
                : [new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true)];
            return Task.FromResult(new NetworkScanner(
                new FakeNetworkInfoProvider(Info),
                new NetworkSweeper(new NoOpHostProber()),
                new FakeArpTableReader(entries),
                new FakeOuiVendorLookup(),
                new FakePingTimeMeasurer(),
                new FakePortScanner()));
        }
        var vm = BuildViewModel(Provider, deviceNotifier: notifier);

        await vm.RefreshCommand.ExecuteAsync(null);
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("192.168.1.20", notifier.NotifiedDevices.Single().IpAddress);
    }

    [Fact]
    public async Task RefreshCommand_DoesNotNotify_ARandomizedMacDeviceWithNoLabel_OnALaterScan()
    {
        var notifier = new FakeDeviceNotifier();
        var scanCount = 0;
        Task<NetworkScanner> Provider()
        {
            scanCount++;
            IReadOnlyList<NeighborEntry> entries = scanCount == 1
                ? []
                : [new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true)];
            return Task.FromResult(new NetworkScanner(
                new FakeNetworkInfoProvider(Info),
                new NetworkSweeper(new NoOpHostProber()),
                new FakeArpTableReader(entries),
                new FakeOuiVendorLookup(TsvOuiVendorLookup.RandomizedMacVendor),
                new FakePingTimeMeasurer(),
                new FakePortScanner()));
        }
        var vm = BuildViewModel(Provider, deviceNotifier: notifier);

        await vm.RefreshCommand.ExecuteAsync(null);
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Empty(notifier.NotifiedDevices);
    }

    [Fact]
    public async Task ClearingTheLabel_RemovesARandomizedMacDevice_ThatWasOnlyKeptBecauseOfTheLabel()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Celular Ariel");
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(
            ScannerProvider(Info, new FakeArpTableReader(entries), new FakeOuiVendorLookup(TsvOuiVendorLookup.RandomizedMacVendor)),
            store);
        await vm.RefreshCommand.ExecuteAsync(null);

        vm.Devices.Single(d => d.MacAddress == "aa:bb:cc:dd:ee:ff").Label = "";

        Assert.Null(store.GetRecord("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public async Task ClearingTheLabel_RemovesARegularDevice_EvenWithSightingHistory()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora");
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(ScannerProvider(Info, new FakeArpTableReader(entries)), store);
        await vm.RefreshCommand.ExecuteAsync(null);

        vm.Devices.Single(d => d.MacAddress == "aa:bb:cc:dd:ee:ff").Label = "";

        Assert.Null(store.GetRecord("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public void Constructor_DoesNotSeed_AnUnlabeledPreviouslySeenDevice()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLastSeen("aa:bb:cc:dd:ee:ff", "192.168.1.20", "Acme Inc.");

        var vm = BuildViewModel(ScannerProvider(Info), store);

        Assert.Empty(vm.Devices);
    }

    [Fact]
    public void Constructor_RemovesAnUnlabeledPreviouslySeenDevice_FromTheStore()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLastSeen("aa:bb:cc:dd:ee:ff", "192.168.1.20", "Acme Inc.");

        _ = BuildViewModel(ScannerProvider(Info), store);

        Assert.Null(store.GetRecord("aa:bb:cc:dd:ee:ff"));
    }

    [Fact]
    public void Constructor_RestoresAutoRefreshSettings_FromTheSettingsStore()
    {
        var settingsStore = new FakeAppSettingsStore(new AppSettings(AutoRefreshEnabled: true, AutoRefreshIntervalSeconds: 45));

        var vm = BuildViewModel(ScannerProvider(Info), settingsStore: settingsStore);

        Assert.Equal(45, vm.AutoRefreshIntervalSeconds);
    }

    [Fact]
    public void SettingAutoRefreshEnabled_PersistsItToTheSettingsStore()
    {
        var settingsStore = new FakeAppSettingsStore();
        var vm = BuildViewModel(ScannerProvider(Info), settingsStore: settingsStore);

        vm.AutoRefreshEnabled = true;

        Assert.True(settingsStore.Load().AutoRefreshEnabled);
    }

    [Fact]
    public void SettingAutoRefreshEnabled_PreservesTheWindowSize_AlreadyInTheSettingsStore()
    {
        var settingsStore = new FakeAppSettingsStore(AppSettings.Default with { WindowWidth = 1024 });
        var vm = BuildViewModel(ScannerProvider(Info), settingsStore: settingsStore);

        vm.AutoRefreshEnabled = true;

        Assert.Equal(1024, settingsStore.Load().WindowWidth);
    }

    [Fact]
    public void ApplyVendorLookup_FillsInVendor_ForADeviceSeededWithoutOne()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora");
        var vm = BuildViewModel(ScannerProvider(Info), store);

        vm.ApplyVendorLookup(new FakeOuiVendorLookup("Acme Inc."));

        Assert.Equal("Acme Inc.", vm.Devices.Single().Vendor);
    }

    [Fact]
    public void ApplyVendorLookup_PersistsTheResolvedVendor_ToTheRecordStore()
    {
        var store = new FakeDeviceRecordStore();
        store.SetLabel("aa:bb:cc:dd:ee:ff", "Impresora");
        var vm = BuildViewModel(ScannerProvider(Info), store);

        vm.ApplyVendorLookup(new FakeOuiVendorLookup("Acme Inc."));

        Assert.Equal("Acme Inc.", store.GetRecord("aa:bb:cc:dd:ee:ff")?.LastKnownVendor);
    }

    [Fact]
    public async Task ApplyVendorLookup_DoesNotOverwrite_AVendorAlreadyKnownFromAScan()
    {
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var vm = BuildViewModel(ScannerProvider(Info, new FakeArpTableReader(entries)));
        await vm.RefreshCommand.ExecuteAsync(null);

        vm.ApplyVendorLookup(new FakeOuiVendorLookup("Should Not Apply"));

        var device = vm.Devices.Single(d => d.MacAddress == "aa:bb:cc:dd:ee:ff");
        Assert.Equal("Fake Vendor", device.Vendor);
    }
}
