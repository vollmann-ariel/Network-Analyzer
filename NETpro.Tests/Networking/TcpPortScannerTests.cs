using System.Net;
using System.Net.Sockets;
using NETpro.Networking;

namespace NETpro.Tests.Networking;

public class TcpPortScannerTests
{
    [Fact]
    public async Task ScanAsync_FindsAnOpenPort_OnLoopback()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var scanner = new TcpPortScanner([port]);

        var openPorts = await scanner.ScanAsync(IPAddress.Loopback, CancellationToken.None);

        Assert.Contains(port, openPorts);
    }

    [Fact]
    public async Task ScanAsync_DoesNotReportAClosedPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var closedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var scanner = new TcpPortScanner([closedPort]);

        var openPorts = await scanner.ScanAsync(IPAddress.Loopback, CancellationToken.None);

        Assert.DoesNotContain(closedPort, openPorts);
    }
}
