using System.Net;

namespace NETpro.Networking;

public sealed class NetworkSweeper(IHostProber prober, int maxConcurrency = 64)
{
    public async Task SweepAsync(IReadOnlyList<IPAddress> hosts, CancellationToken ct = default)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = hosts.Select(async host =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await prober.ProbeAsync(host, ct);
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
    }
}
