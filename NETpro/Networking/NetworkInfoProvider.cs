using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NETpro.Networking;

public sealed record LocalNetworkInfo(IPAddress IpAddress, int PrefixLength, string InterfaceName, int InterfaceIndex);

public interface INetworkInfoProvider
{
    LocalNetworkInfo? GetActiveNetworkInfo();
}

/// <summary>
/// A PC can have Ethernet, Wi-Fi, VPN, and virtual (Hyper-V/VMware) adapters up simultaneously,
/// unlike Android's single wlan0. Restricting to adapters with an IPv4 default gateway excludes
/// isolated virtual switches; physical Ethernet/Wi-Fi adapters are preferred when more than one
/// still qualifies.
/// </summary>
public sealed class SystemNetworkInfoProvider : INetworkInfoProvider
{
    public LocalNetworkInfo? GetActiveNetworkInfo()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                          && nic.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
            .Select(nic => (nic, props: nic.GetIPProperties()))
            .Where(x => x.props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
            .OrderByDescending(x => x.nic.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211 ? 1 : 0)
            .Select(x => (x.nic, addr: x.props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)))
            .Where(x => x.addr is not null)
            .Select(x => new LocalNetworkInfo(
                x.addr!.Address,
                x.addr.PrefixLength,
                x.nic.Name,
                x.nic.GetIPProperties().GetIPv4Properties()?.Index ?? -1))
            .FirstOrDefault();
    }
}
