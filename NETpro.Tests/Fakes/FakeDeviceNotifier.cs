using NETpro.Models;
using NETpro.Notifications;

namespace NETpro.Tests.Fakes;

public sealed class FakeDeviceNotifier : IDeviceNotifier
{
    public List<DeviceInfo> NotifiedDevices { get; } = [];

    public void NotifyNewDevice(DeviceInfo device) => NotifiedDevices.Add(device);
}
