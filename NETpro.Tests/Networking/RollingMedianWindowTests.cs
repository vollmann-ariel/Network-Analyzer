using NETpro.Networking;

namespace NETpro.Tests.Networking;

public class RollingMedianWindowTests
{
    [Fact]
    public void Median_ReturnsNull_WhenEmpty()
    {
        var window = new RollingMedianWindow(capacity: 5);
        Assert.Null(window.Median);
    }

    [Fact]
    public void Median_ReturnsMiddleValue_ForOddSampleCount()
    {
        var window = new RollingMedianWindow(capacity: 5);
        foreach (var sample in new long[] { 10, 50, 20 }) window.Add(sample);
        Assert.Equal(20, window.Median);
    }

    [Fact]
    public void Median_ReturnsAverageOfMiddleTwo_ForEvenSampleCount()
    {
        var window = new RollingMedianWindow(capacity: 5);
        foreach (var sample in new long[] { 10, 20, 30, 40 }) window.Add(sample);
        Assert.Equal(25, window.Median);
    }

    [Fact]
    public void Median_IgnoresOneOffSpike_UnlikeAMean()
    {
        var window = new RollingMedianWindow(capacity: 5);
        foreach (var sample in new long[] { 10, 11, 12, 500 }) window.Add(sample);
        Assert.Equal(11, window.Median);
    }

    [Fact]
    public void Add_DropsOldestSample_BeyondCapacity()
    {
        var window = new RollingMedianWindow(capacity: 3);
        foreach (var sample in new long[] { 1, 1, 1, 999 }) window.Add(sample);
        Assert.Equal(1, window.Median);
    }
}
