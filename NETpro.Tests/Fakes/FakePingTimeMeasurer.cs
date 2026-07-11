using System.Net;
using NETpro.Networking;

namespace NETpro.Tests.Fakes;

public sealed class FakePingTimeMeasurer(long? fixedResult = 10) : IPingTimeMeasurer
{
    public Task<long?> MeasureAverageRoundtripAsync(IPAddress host, int sampleCount, CancellationToken ct) =>
        Task.FromResult(fixedResult);
}
