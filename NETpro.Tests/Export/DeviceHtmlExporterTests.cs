using NETpro.Export;
using NETpro.Models;

namespace NETpro.Tests.Export;

public class DeviceHtmlExporterTests
{
    [Fact]
    public void ToHtml_IncludesTheColumnHeaders()
    {
        var html = DeviceHtmlExporter.ToHtml([]);
        Assert.Contains("<th>IP</th><th>MAC</th><th>Fabricante</th><th>Ping</th><th>Referencia</th>", html);
    }

    [Fact]
    public void ToHtml_WritesARowWithEachDevicesFields()
    {
        var device = new DeviceInfo("192.168.1.20", "aa:bb:cc:dd:ee:ff", "Acme Inc.", isSelf: false)
        {
            PingTimeMs = 12,
            Label = "Impresora"
        };
        var html = DeviceHtmlExporter.ToHtml([device]);
        Assert.Contains("<td>192.168.1.20</td><td>aa:bb:cc:dd:ee:ff</td><td>Acme Inc.</td><td>12 ms</td><td>Impresora</td>", html);
    }

    [Fact]
    public void ToHtml_EscapesHtmlSpecialCharacters_InAReferenceLabel()
    {
        var device = new DeviceInfo("192.168.1.20", "aa:bb:cc:dd:ee:ff", "Acme Inc.", isSelf: false)
        {
            Label = "<script>alert(1)</script>"
        };
        var html = DeviceHtmlExporter.ToHtml([device]);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
    }
}
