using System.Text;
using NETpro.Models;

namespace NETpro.Export;

public static class DeviceCsvExporter
{
    public static string ToCsv(IEnumerable<DeviceInfo> devices)
    {
        var sb = new StringBuilder();
        sb.AppendLine("IP,MAC,Fabricante,Ping,Referencia");
        foreach (var device in devices)
        {
            sb.AppendLine(string.Join(",",
                EscapeField(device.IpAddress),
                EscapeField(device.MacAddress ?? string.Empty),
                EscapeField(device.Vendor ?? string.Empty),
                EscapeField(device.PingDisplay),
                EscapeField(device.Label)));
        }
        return sb.ToString();
    }

    private static string EscapeField(string field) =>
        field.IndexOfAny([',', '"', '\n', '\r']) < 0 ? field : $"\"{field.Replace("\"", "\"\"")}\"";
}
