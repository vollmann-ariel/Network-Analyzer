using System.Net;

namespace NETpro.Networking;

public static class SubnetCalculator
{
    public static IReadOnlyList<IPAddress> HostAddressesInSubnet(IPAddress ip, int prefixLength)
    {
        var ipValue = ToUInt32(ip.GetAddressBytes());
        var mask = prefixLength <= 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        var network = ipValue & mask;
        var broadcast = network | ~mask;

        var hosts = new List<IPAddress>();
        for (var addr = network + 1; addr < broadcast; addr++)
        {
            hosts.Add(ToIPAddress(addr));
        }
        return hosts;
    }

    private static uint ToUInt32(byte[] bytes) =>
        ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

    private static IPAddress ToIPAddress(uint value) =>
        new(new[]
        {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        });
}
