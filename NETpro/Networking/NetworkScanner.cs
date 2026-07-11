using NETpro.Models;
using NETpro.Oui;

namespace NETpro.Networking;

public abstract record ScanResult
{
    public sealed record Success(IReadOnlyList<DeviceInfo> Devices) : ScanResult;
    public sealed record NoActiveNetwork : ScanResult;
    public sealed record SubnetTooLarge(int HostCount) : ScanResult;
    public sealed record ArpUnavailable : ScanResult;
}

public sealed class NetworkScanner(
    INetworkInfoProvider networkInfoProvider,
    NetworkSweeper sweeper,
    IArpTableReader arpTableReader,
    IOuiVendorLookup vendorLookup)
{
    public const int MaxScanHosts = 1024;

    public async Task<ScanResult> ScanAsync(CancellationToken ct = default)
    {
        var info = networkInfoProvider.GetActiveNetworkInfo();
        if (info is null) return new ScanResult.NoActiveNetwork();

        var hosts = SubnetCalculator.HostAddressesInSubnet(info.IpAddress, info.PrefixLength);
        if (hosts.Count > MaxScanHosts) return new ScanResult.SubnetTooLarge(hosts.Count);

        await sweeper.SweepAsync(hosts.Where(h => !h.Equals(info.IpAddress)).ToList(), ct);

        IReadOnlyList<NeighborEntry> entries;
        try
        {
            entries = arpTableReader.ReadEntries();
        }
        catch (ArpTableUnavailableException)
        {
            return new ScanResult.ArpUnavailable();
        }

        // GetIpNetTable2 also returns pre-existing multicast/broadcast neighbor entries
        // (224.0.0.0/4, 255.255.255.255) unrelated to the sweep — restrict to addresses
        // actually within the scanned subnet so those don't show up as "devices".
        var hostSet = hosts.ToHashSet();
        var others = entries
            .Where(e => e.InterfaceIndex == info.InterfaceIndex && e.IsResolved && hostSet.Contains(e.IpAddress))
            .Select(e => new DeviceInfo(e.IpAddress.ToString(), e.MacAddress, vendorLookup.Lookup(e.MacAddress), IsSelf: false))
            .OrderBy(d => IpSortKey(d.IpAddress))
            .ToList();

        var self = new DeviceInfo(info.IpAddress.ToString(), MacAddress: null, Vendor: null, IsSelf: true);
        return new ScanResult.Success(new[] { self }.Concat(others).ToList());
    }

    private static long IpSortKey(string ip) =>
        ip.Split('.').Aggregate(0L, (acc, part) => acc * 256 + (int.TryParse(part, out var n) ? n : 0));
}
