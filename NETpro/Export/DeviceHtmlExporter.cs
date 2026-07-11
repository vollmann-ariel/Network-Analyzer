using System.Net;
using System.Text;
using NETpro.Models;

namespace NETpro.Export;

public static class DeviceHtmlExporter
{
    public static string ToHtml(IEnumerable<DeviceInfo> devices)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"es\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<title>Dispositivos en la red local</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: sans-serif; margin: 2rem; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
        sb.AppendLine("th, td { border: 1px solid #ccc; padding: 6px 10px; text-align: left; }");
        sb.AppendLine("th { background: #f0f0f0; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>Dispositivos en la red local</h1>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>IP</th><th>MAC</th><th>Fabricante</th><th>Ping</th><th>Referencia</th></tr>");
        foreach (var device in devices)
        {
            sb.AppendLine(
                $"<tr><td>{Encode(device.IpAddress)}</td><td>{Encode(device.MacAddress ?? "")}</td>" +
                $"<td>{Encode(device.Vendor ?? "")}</td><td>{Encode(device.PingDisplay)}</td><td>{Encode(device.Label)}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
