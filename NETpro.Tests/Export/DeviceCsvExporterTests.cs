using NETpro.Export;
using NETpro.Models;

namespace NETpro.Tests.Export;

public class DeviceCsvExporterTests
{
    [Fact]
    public void ToCsv_StartsWithTheHeaderRow()
    {
        var csv = DeviceCsvExporter.ToCsv([]);
        Assert.StartsWith("IP,MAC,Fabricante,Ping,Referencia", csv);
    }

    [Fact]
    public void ToCsv_WritesOneRowPerDevice_InColumnOrder()
    {
        var device = new DeviceInfo("192.168.1.20", "aa:bb:cc:dd:ee:ff", "Acme Inc.", isSelf: false)
        {
            PingTimeMs = 12,
            Label = "Impresora"
        };
        var csv = DeviceCsvExporter.ToCsv([device]);
        Assert.Contains("192.168.1.20,aa:bb:cc:dd:ee:ff,Acme Inc.,12 ms,Impresora", csv);
    }

    [Fact]
    public void ToCsv_QuotesAndEscapesAFieldContainingACommaAndQuote()
    {
        var device = new DeviceInfo("192.168.1.20", "aa:bb:cc:dd:ee:ff", "Acme Inc.", isSelf: false)
        {
            Label = "Router \"grande\", oficina"
        };
        var csv = DeviceCsvExporter.ToCsv([device]);
        Assert.Contains("\"Router \"\"grande\"\", oficina\"", csv);
    }
}
