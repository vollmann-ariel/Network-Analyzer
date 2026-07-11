using NETpro.Networking;

namespace NETpro.Tests.Fakes;

public sealed class ThrowingArpTableReader : IArpTableReader
{
    public IReadOnlyList<NeighborEntry> ReadEntries() => throw new ArpTableUnavailableException(new InvalidOperationException("fake failure"));
}
