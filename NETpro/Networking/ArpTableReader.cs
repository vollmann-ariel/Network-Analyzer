using System.Net;
using Vanara.PInvoke;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

namespace NETpro.Networking;

public sealed record NeighborEntry(IPAddress IpAddress, string MacAddress, int InterfaceIndex, bool IsResolved);

public class ArpTableUnavailableException(Exception inner) : Exception("Failed to read the IP neighbor table.", inner);

public interface IArpTableReader
{
    IReadOnlyList<NeighborEntry> ReadEntries();
}

/// <summary>
/// Reads the kernel IP neighbor (ARP) table via GetIpNetTable2 — no elevation required for a
/// normal user process on Windows, unlike Android where this is blocked outright since
/// Android 13 (see the abandoned Android version's NetlinkArpTableReader for that story).
/// </summary>
public sealed class IpHlpApiArpTableReader : IArpTableReader
{
    public IReadOnlyList<NeighborEntry> ReadEntries()
    {
        try
        {
            GetIpNetTable2(ADDRESS_FAMILY.AF_INET, out var table).ThrowIfFailed();
            using (table)
            {
                return table
                    .Where(row => row.Address.si_family == ADDRESS_FAMILY.AF_INET)
                    .Select(ToNeighborEntry)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            throw new ArpTableUnavailableException(ex);
        }
    }

    private static NeighborEntry ToNeighborEntry(MIB_IPNET_ROW2 row)
    {
        var ipBytes = (byte[])row.Address.Ipv4.sin_addr;
        var mac = FormatMac(row.PhysicalAddress, (int)row.PhysicalAddressLength);
        var resolved = row.State is NL_NEIGHBOR_STATE.NlnsReachable or NL_NEIGHBOR_STATE.NlnsStale
            or NL_NEIGHBOR_STATE.NlnsDelay or NL_NEIGHBOR_STATE.NlnsPermanent or NL_NEIGHBOR_STATE.NlnsProbe;
        return new NeighborEntry(new IPAddress(ipBytes), mac, (int)row.InterfaceIndex, resolved && mac != "00:00:00:00:00:00");
    }

    private static string FormatMac(byte[]? physicalAddress, int length) =>
        physicalAddress is not null && length == 6
            ? string.Join(":", physicalAddress.Take(6).Select(b => b.ToString("x2")))
            : "00:00:00:00:00:00";
}
