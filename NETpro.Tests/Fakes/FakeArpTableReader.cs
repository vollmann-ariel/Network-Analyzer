using NETpro.Networking;

namespace NETpro.Tests.Fakes;

public sealed class FakeArpTableReader(IReadOnlyList<NeighborEntry> entries) : IArpTableReader
{
    public IReadOnlyList<NeighborEntry> ReadEntries() => entries;
}
