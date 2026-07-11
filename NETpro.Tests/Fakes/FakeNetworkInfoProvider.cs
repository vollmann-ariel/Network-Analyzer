using NETpro.Networking;

namespace NETpro.Tests.Fakes;

public sealed class FakeNetworkInfoProvider(LocalNetworkInfo? info) : INetworkInfoProvider
{
    public LocalNetworkInfo? GetActiveNetworkInfo() => info;
}
