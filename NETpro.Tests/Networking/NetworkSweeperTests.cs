using System.Net;
using NETpro.Networking;

namespace NETpro.Tests.Networking;

public class NetworkSweeperTests
{
    private sealed class ConcurrencyTrackingHostProber : IHostProber
    {
        private int _current;
        public int PeakConcurrency { get; private set; }
        private readonly object _lock = new();

        public async Task ProbeAsync(IPAddress host, CancellationToken ct)
        {
            lock (_lock)
            {
                _current++;
                if (_current > PeakConcurrency) PeakConcurrency = _current;
            }
            await Task.Delay(10, ct);
            lock (_lock) { _current--; }
        }
    }

    [Fact]
    public async Task SweepAsync_NeverExceedsMaxConcurrency()
    {
        var prober = new ConcurrencyTrackingHostProber();
        var sweeper = new NetworkSweeper(prober, maxConcurrency: 8);
        var hosts = Enumerable.Range(1, 50).Select(i => IPAddress.Parse($"10.0.0.{i}")).ToList();

        await sweeper.SweepAsync(hosts);

        Assert.True(prober.PeakConcurrency <= 8);
    }
}
