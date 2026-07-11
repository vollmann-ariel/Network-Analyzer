using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;

namespace NETpro.Networking;

public interface IPingTimeMeasurer
{
    Task<long?> MeasureAsync(IPAddress host, int sampleCount, CancellationToken ct);
}

/// <summary>
/// A shorter timeout than HostProber's is fine here: by the time this runs, the host has
/// already appeared in the IP neighbor table, so it's known to be alive on the LAN — a
/// non-responsive sample means ICMP is filtered, not that the host is slow to reach.
///
/// Samples accumulate in a per-host rolling window across calls (i.e. across scan cycles),
/// so the reported value smooths out with repeated scans instead of resetting each time.
/// </summary>
public sealed class IcmpPingTimeMeasurer(int timeoutMs = 500, int windowCapacity = 20) : IPingTimeMeasurer
{
    private readonly ConcurrentDictionary<IPAddress, RollingMedianWindow> _windows = new();

    public async Task<long?> MeasureAsync(IPAddress host, int sampleCount, CancellationToken ct)
    {
        using var ping = new Ping();
        var window = _windows.GetOrAdd(host, _ => new RollingMedianWindow(windowCapacity));
        for (var i = 0; i < sampleCount; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync(host, timeoutMs).WaitAsync(ct);
                if (reply.Status == IPStatus.Success) window.Add(reply.RoundtripTime);
            }
            catch (PingException)
            {
            }
        }
        return window.Median;
    }
}
