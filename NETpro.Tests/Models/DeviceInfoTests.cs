using NETpro.Models;

namespace NETpro.Tests.Models;

public class DeviceInfoTests
{
    private static DeviceInfo Device(params int[] openPorts) =>
        new("192.168.1.20", "aa:bb:cc:dd:ee:ff", "Acme Inc.", isSelf: false) { OpenPorts = openPorts };

    [Fact]
    public void ServicesDisplay_ListsKnownPortNames()
    {
        Assert.Equal("HTTP, SSH", Device(80, 22).ServicesDisplay);
    }

    [Fact]
    public void ServicesDisplay_IsEmpty_WhenNoPortsAreOpen()
    {
        Assert.Equal("", Device().ServicesDisplay);
    }

    [Fact]
    public void OpenPortsDisplay_ListsRawPortNumbers()
    {
        Assert.Equal("22, 80", Device(22, 80).OpenPortsDisplay);
    }

    [Fact]
    public void OpenPortsDisplay_IsEmpty_WhenNoPortsAreOpen()
    {
        Assert.Equal("", Device().OpenPortsDisplay);
    }
}
