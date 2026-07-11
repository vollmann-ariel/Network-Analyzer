using System.Net;
using System.Net.NetworkInformation;

namespace NETpro.Networking;

/// <summary>
/// Sending an echo request forces the local network stack to ARP-resolve the destination
/// before it can even put the packet on the wire, regardless of whether the target answers.
/// A timeout/PingException is an expected, harmless outcome — the side effect that matters
/// (a populated IP neighbor table entry) has already happened by the time the call returns.
/// </summary>
public interface IHostProber
{
    Task ProbeAsync(IPAddress host, CancellationToken ct);
}

public sealed class PingHostProber(int timeoutMs = 500) : IHostProber
{
    public async Task ProbeAsync(IPAddress host, CancellationToken ct)
    {
        using var ping = new Ping();
        try
        {
            await ping.SendPingAsync(host, timeoutMs).WaitAsync(ct);
        }
        catch (PingException)
        {
        }
    }
}
