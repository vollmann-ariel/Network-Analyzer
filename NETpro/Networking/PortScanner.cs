using System.Net;
using System.Net.Sockets;

namespace NETpro.Networking;

public interface IPortScanner
{
    Task<IReadOnlyList<int>> ScanAsync(IPAddress host, CancellationToken ct);
}

/// <summary>
/// A TCP connect scan, not a raw SYN scan — the latter needs raw sockets (admin rights on
/// Windows), which this app deliberately avoids elsewhere too. The short per-port timeout is
/// safe because a closed/filtered port on an already-known-alive host (it's in the neighbor
/// table) resolves fast; a real service usually accepts within a handful of milliseconds.
/// </summary>
public sealed class TcpPortScanner(IReadOnlyList<int>? ports = null, int timeoutMs = 300) : IPortScanner
{
    private readonly IReadOnlyList<int> _ports = ports ?? WellKnownPorts.Names.Keys.ToList();

    public async Task<IReadOnlyList<int>> ScanAsync(IPAddress host, CancellationToken ct)
    {
        var results = await Task.WhenAll(_ports.Select(port => TryConnectAsync(host, port, ct)));
        return results.Where(p => p is not null).Select(p => p!.Value).OrderBy(p => p).ToList();
    }

    private async Task<int?> TryConnectAsync(IPAddress host, int port, CancellationToken ct)
    {
        using var client = new TcpClient();
        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        try
        {
            await client.ConnectAsync(host, port, linked.Token);
            return port;
        }
        catch (SocketException)
        {
            return null;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return null;
        }
    }
}
