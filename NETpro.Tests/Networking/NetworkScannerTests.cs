using System.Net;
using NETpro.Networking;
using NETpro.Tests.Fakes;

namespace NETpro.Tests.Networking;

public class NetworkScannerTests
{
    private static readonly IPAddress SelfIp = IPAddress.Parse("192.168.1.10");
    private static readonly LocalNetworkInfo Info = new(SelfIp, 24, "Ethernet", InterfaceIndex: 5);
    private static readonly NetworkSweeper Sweeper = new(new NoOpHostProber());
    private static readonly FakeOuiVendorLookup VendorLookup = new("Fake Vendor");
    private static readonly FakePingTimeMeasurer PingTimeMeasurer = new(10);
    private static readonly FakePortScanner PortScanner = new();

    private static NetworkScanner ScannerWith(IReadOnlyList<NeighborEntry> entries, LocalNetworkInfo? info = null) =>
        new(
            new FakeNetworkInfoProvider(info ?? Info),
            Sweeper,
            new FakeArpTableReader(entries),
            VendorLookup,
            PingTimeMeasurer,
            PortScanner);

    [Fact]
    public async Task Scan_IncludesSelfEntry_WithNullMac()
    {
        var result = (ScanResult.Success)await ScannerWith([]).ScanAsync();
        var self = result.Devices.Single(d => d.IsSelf);
        Assert.Null(self.MacAddress);
    }

    [Fact]
    public async Task Scan_ExcludesEntries_FromDifferentInterface()
    {
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 99, IsResolved: true) };
        var result = (ScanResult.Success)await ScannerWith(entries).ScanAsync();
        Assert.DoesNotContain(result.Devices, d => d.IpAddress == "192.168.1.20");
    }

    [Fact]
    public async Task Scan_ExcludesUnresolvedEntries()
    {
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "00:00:00:00:00:00", InterfaceIndex: 5, IsResolved: false) };
        var result = (ScanResult.Success)await ScannerWith(entries).ScanAsync();
        Assert.DoesNotContain(result.Devices, d => d.IpAddress == "192.168.1.20");
    }

    [Fact]
    public async Task Scan_SortsDevicesByIpAscending()
    {
        var entries = new[]
        {
            new NeighborEntry(IPAddress.Parse("192.168.1.100"), "aa:bb:cc:dd:ee:01", InterfaceIndex: 5, IsResolved: true),
            new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:02", InterfaceIndex: 5, IsResolved: true)
        };
        var result = (ScanResult.Success)await ScannerWith(entries).ScanAsync();
        var otherIps = result.Devices.Where(d => !d.IsSelf).Select(d => d.IpAddress).ToList();
        Assert.Equal(["192.168.1.20", "192.168.1.100"], otherIps);
    }

    [Fact]
    public async Task Scan_ReturnsSubnetTooLarge_AboveHostCap()
    {
        var largeInfo = new LocalNetworkInfo(SelfIp, 20, "Ethernet", InterfaceIndex: 5);
        var result = await ScannerWith([], largeInfo).ScanAsync();
        Assert.IsType<ScanResult.SubnetTooLarge>(result);
    }

    [Fact]
    public async Task Scan_AttachesMeasuredPingTime_ToEachDevice()
    {
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var result = (ScanResult.Success)await ScannerWith(entries).ScanAsync();
        var other = result.Devices.Single(d => !d.IsSelf);
        Assert.Equal(10, other.PingTimeMs);
    }

    [Fact]
    public async Task Scan_ReturnsNoActiveNetwork_WhenNetworkInfoMissing()
    {
        var scanner = new NetworkScanner(new FakeNetworkInfoProvider(null), Sweeper, new FakeArpTableReader([]), VendorLookup, PingTimeMeasurer, PortScanner);
        var result = await scanner.ScanAsync();
        Assert.IsType<ScanResult.NoActiveNetwork>(result);
    }

    [Fact]
    public async Task Scan_AttachesOpenPorts_ToEachDevice()
    {
        var entries = new[] { new NeighborEntry(IPAddress.Parse("192.168.1.20"), "aa:bb:cc:dd:ee:ff", InterfaceIndex: 5, IsResolved: true) };
        var scanner = new NetworkScanner(
            new FakeNetworkInfoProvider(Info), Sweeper, new FakeArpTableReader(entries), VendorLookup, PingTimeMeasurer,
            new FakePortScanner([80, 443]));

        var result = (ScanResult.Success)await scanner.ScanAsync();

        var other = result.Devices.Single(d => !d.IsSelf);
        Assert.Equal([80, 443], other.OpenPorts);
    }
}
