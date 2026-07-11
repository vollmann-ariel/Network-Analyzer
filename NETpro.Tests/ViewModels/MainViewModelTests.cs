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
            new FakeOuiVendorLookup()));

    [Fact]
    public async Task RefreshCommand_PopulatesDevices_OnSuccess()
    {
        var vm = new MainViewModel(ScannerProvider(Info));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.Single(vm.Devices);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenNoActiveNetwork()
    {
        var vm = new MainViewModel(ScannerProvider(null));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenSubnetTooLarge()
    {
        var largeInfo = new LocalNetworkInfo(IPAddress.Parse("192.168.1.10"), 20, "Ethernet", 5);
        var vm = new MainViewModel(ScannerProvider(largeInfo));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsErrorMessage_WhenArpUnavailable()
    {
        var vm = new MainViewModel(ScannerProvider(Info, new ThrowingArpTableReader()));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public async Task RefreshCommand_SetsIsLoadingFalse_AfterCompletion()
    {
        var vm = new MainViewModel(ScannerProvider(Info));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task RefreshCommand_SetsHasScannedTrue_AfterCompletion()
    {
        var vm = new MainViewModel(ScannerProvider(Info));
        await vm.RefreshCommand.ExecuteAsync(null);
        Assert.True(vm.HasScanned);
    }
}
