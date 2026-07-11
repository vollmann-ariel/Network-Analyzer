using System.Net;
using NETpro.Networking;
using NETpro.Tests.Fakes;
using NETpro.ViewModels;

namespace NETpro.Tests.ViewModels;

public class MainViewModelTests
{
    private static readonly LocalNetworkInfo Info = new(IPAddress.Parse("192.168.1.10"), 24, "Ethernet", 5);

    private static Func<Task<NetworkScanner>> ScannerProvider(
        LocalNetworkInfo? info,
        IArpTableReader? arpTableReader = null) =>
        () => Task.FromResult(new NetworkScanner(
            new FakeNetworkInfoProvider(info),
            new NetworkSweeper(new NoOpHostProber()),
            arpTableReader ?? new FakeArpTableReader([]),
            new FakeOuiVendorLookup(),
            new FakePingTimeMeasurer()));

    [Fact]
    public async Task RefreshCommand_PopulatesDevices_OnSuccess()
    {
        var vm = new MainViewModel(ScannerProvider(Info), new FakeDeviceLabelStore());
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Single(vm.Devices);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenNoActiveNetwork()
    {
        var vm = new MainViewModel(ScannerProvider(null), new FakeDeviceLabelStore());
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenSubnetTooLarge()
    {
        var largeInfo = new LocalNetworkInfo(IPAddress.Parse("192.168.1.10"), 20, "Ethernet", 5);
        var vm = new MainViewModel(ScannerProvider(largeInfo), new FakeDeviceLabelStore());
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenArpUnavailable()
    {
        var vm = new MainViewModel(ScannerProvider(Info, new ThrowingArpTableReader()), new FakeDeviceLabelStore());
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsIsLoadingFalse_AfterCompletion()
    {
        var vm = new MainViewModel(ScannerProvider(Info), new FakeDeviceLabelStore());
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task RefreshCommand_SetsHasScannedTrue_AfterCompletion()
    {
        var vm = new MainViewModel(ScannerProvider(Info), new FakeDeviceLabelStore());
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.True(vm.HasScanned);
    }

    [Fact]
    public async Task RefreshCommand_DefaultsSelfLabel_ToEsteEquipo()
    {
        var vm = new MainViewModel(ScannerProvider(Info), new FakeDeviceLabelStore());
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Equal("Este equipo", vm.Devices.Single(d => d.IsSelf).Label);
    }

    [Fact]
    public async Task RefreshCommand_RestoresPersistedLabel_OnNextScan()
    {
        var store = new FakeDeviceLabelStore();
        var vm = new MainViewModel(ScannerProvider(Info), store);
        await vm.RefreshCommand.ExecuteAsync(null);
        vm.Devices.Single(d => d.IsSelf).Label = "Mi PC";

        var vm2 = new MainViewModel(ScannerProvider(Info), store);
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
                new FakePingTimeMeasurer()));
        }

        var vm = new MainViewModel(Provider, new FakeDeviceLabelStore());
        await vm.RefreshCommand.ExecuteAsync(null);
        await vm.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Devices.Count);
    }
}
