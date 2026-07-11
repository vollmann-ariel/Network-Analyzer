using System.Net;
using NETpro.Networking;

namespace NETpro.Tests.Networking;

public class SubnetCalculatorTests
{
    private static readonly IPAddress Ip = IPAddress.Parse("192.168.1.10");

    [Fact]
    public void Slash24_Returns254HostAddresses()
    {
        var hosts = SubnetCalculator.HostAddressesInSubnet(Ip, 24);
        Assert.Equal(254, hosts.Count);
    }

    [Fact]
    public void Slash24_ExcludesNetworkAddress()
    {
        var hosts = SubnetCalculator.HostAddressesInSubnet(Ip, 24);
        Assert.DoesNotContain(hosts, h => h.ToString() == "192.168.1.0");
    }

    [Fact]
    public void Slash24_ExcludesBroadcastAddress()
    {
        var hosts = SubnetCalculator.HostAddressesInSubnet(Ip, 24);
        Assert.DoesNotContain(hosts, h => h.ToString() == "192.168.1.255");
    }

    [Fact]
    public void Slash30_ReturnsExactlyTwoUsableHosts()
    {
        var hosts = SubnetCalculator.HostAddressesInSubnet(Ip, 30);
        Assert.Equal(2, hosts.Count);
    }
}
