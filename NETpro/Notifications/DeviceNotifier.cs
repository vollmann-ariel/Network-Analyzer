using System.Windows.Forms;
using NETpro.Models;

namespace NETpro.Notifications;

public interface IDeviceNotifier
{
    void NotifyNewDevice(DeviceInfo device);
}

/// <summary>
/// NotifyIcon.ShowBalloonTip surfaces as a real Windows notification (Action Center on
/// Windows 10/11) without needing an AUMID/shortcut registration, which the modern toast
/// APIs require and which a loose, non-packaged exe like this one doesn't have.
/// </summary>
public sealed class TrayDeviceNotifier : IDeviceNotifier, IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayDeviceNotifier()
    {
        var exePath = Environment.ProcessPath;
        var icon = exePath is not null ? System.Drawing.Icon.ExtractAssociatedIcon(exePath) : null;
        _notifyIcon = new NotifyIcon
        {
            Icon = icon ?? System.Drawing.SystemIcons.Information,
            Visible = true,
            Text = "NETpro"
        };
    }

    public void NotifyNewDevice(DeviceInfo device)
    {
        var vendor = string.IsNullOrEmpty(device.Vendor) ? "Fabricante desconocido" : device.Vendor;
        _notifyIcon.BalloonTipTitle = "Nuevo dispositivo en la red";
        _notifyIcon.BalloonTipText = $"{vendor}\n{device.IpAddress}  ({device.MacAddress})";
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void Dispose() => _notifyIcon.Dispose();
}
