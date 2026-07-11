namespace NETpro.Networking;

/// <summary>
/// Keeps the last N samples and reports their median. Median (not mean) absorbs occasional
/// latency spikes without needing an arbitrary outlier threshold — a mean-based cutoff biases
/// low, since roughly half of any sample set legitimately falls above the running mean.
/// </summary>
public sealed class RollingMedianWindow(int capacity)
{
    private readonly Queue<long> _samples = new();

    public void Add(long value)
    {
        _samples.Enqueue(value);
        if (_samples.Count > capacity) _samples.Dequeue();
    }

    public long? Median
    {
        get
        {
            if (_samples.Count == 0) return null;
            var sorted = _samples.OrderBy(x => x).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }
    }
}
