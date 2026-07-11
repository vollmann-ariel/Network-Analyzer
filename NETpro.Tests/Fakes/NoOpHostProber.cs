using System.Net;
using NETpro.Networking;

namespace NETpro.Tests.Fakes;

public sealed class NoOpHostProber : IHostProber
{
    public Task ProbeAsync(IPAddress host, CancellationToken ct) => Task.CompletedTask;
}
