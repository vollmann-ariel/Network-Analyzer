using System.Net;
using System.Net.NetworkInformation;

namespace NETpro.Networking;

public interface IPingTimeMeasurer
{
    Task<long?> MeasureAverageRoundtripAsync(IPAddress host, int sampleCount, CancellationToken ct);
}

/// <summary>
/// A shorter timeout than HostProber's is fine here: by the time this runs, the host has
/// already appeared in the IP neighbor table, so it's known to be alive on the LAN — a
/// non-responsive sample means ICMP is filtered, not that the host is slow to reach.
/// </summary>
public sealed class IcmpPingTimeMeasurer(int timeoutMs = 500) : IPingTimeMeasurer
{
    public async Task<long?> MeasureAverageRoundtripAsync(IPAddress host, int sampleCount, CancellationToken ct)
    {
        using var ping = new Ping();
        var roundtrips = new List<long>();
        for (var i = 0; i < sampleCount; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync(host, timeoutMs).WaitAsync(ct);
                if (reply.Status == IPStatus.Success) roundtrips.Add(reply.RoundtripTime);
            }
            catch (PingException)
            {
            }
        }
        return roundtrips.Count > 0 ? (long)roundtrips.Average() : null;
    }
}
