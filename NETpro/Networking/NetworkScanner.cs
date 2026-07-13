using System.Collections.Concurrent;
using System.Net;
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
    IOuiVendorLookup vendorLookup,
    IPingTimeMeasurer pingTimeMeasurer,
    IPortScanner portScanner)
{
    public const int MaxScanHosts = 1024;
    public const int PingSamplesPerDevice = 5;
    private const int MaxPingConcurrency = 16;
    private const int MaxPortScanConcurrency = 16;

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
        var otherEntries = entries
            .Where(e => e.InterfaceIndex == info.InterfaceIndex && e.IsResolved && hostSet.Contains(e.IpAddress))
            .OrderBy(e => IpSortKey(e.IpAddress.ToString()))
            .ToList();

        var ips = otherEntries.Select(e => e.IpAddress).Append(info.IpAddress).ToList();
        var pingTimesTask = MeasurePingTimesAsync(ips, ct);
        var openPortsTask = ScanPortsAsync(ips, ct);
        await Task.WhenAll(pingTimesTask, openPortsTask);
        var pingTimes = pingTimesTask.Result;
        var openPorts = openPortsTask.Result;

        var others = otherEntries
            .Select(e => new DeviceInfo(
                e.IpAddress.ToString(), e.MacAddress, vendorLookup.Lookup(e.MacAddress), isSelf: false)
            {
                PingTimeMs = pingTimes.GetValueOrDefault(e.IpAddress),
                OpenPorts = openPorts.GetValueOrDefault(e.IpAddress, [])
            })
            .ToList();

        var self = new DeviceInfo(
            info.IpAddress.ToString(), macAddress: null, vendor: null, isSelf: true)
        {
            PingTimeMs = pingTimes.GetValueOrDefault(info.IpAddress),
            OpenPorts = openPorts.GetValueOrDefault(info.IpAddress, [])
        };

        return new ScanResult.Success(new[] { self }.Concat(others).ToList());
    }

    private async Task<IReadOnlyDictionary<IPAddress, long?>> MeasurePingTimesAsync(IReadOnlyList<IPAddress> ips, CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(MaxPingConcurrency);
        var results = new ConcurrentDictionary<IPAddress, long?>();
        var tasks = ips.Select(async ip =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                results[ip] = await pingTimeMeasurer.MeasureAsync(ip, PingSamplesPerDevice, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
        return results;
    }

    private async Task<IReadOnlyDictionary<IPAddress, IReadOnlyList<int>>> ScanPortsAsync(IReadOnlyList<IPAddress> ips, CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(MaxPortScanConcurrency);
        var results = new ConcurrentDictionary<IPAddress, IReadOnlyList<int>>();
        var tasks = ips.Select(async ip =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                results[ip] = await portScanner.ScanAsync(ip, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
        return results;
    }

    private static long IpSortKey(string ip) =>
        ip.Split('.').Aggregate(0L, (acc, part) => acc * 256 + (int.TryParse(part, out var n) ? n : 0));
}
