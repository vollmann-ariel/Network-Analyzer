using System.Net;
using NETpro.Networking;

namespace NETpro.Tests.Fakes;

public sealed class FakePortScanner(IReadOnlyList<int>? fixedResult = null) : IPortScanner
{
    public Task<IReadOnlyList<int>> ScanAsync(IPAddress host, CancellationToken ct) =>
        Task.FromResult(fixedResult ?? (IReadOnlyList<int>)[]);
}
