namespace NETpro.Models;

public sealed record DeviceInfo(string IpAddress, string? MacAddress, string? Vendor, bool IsSelf);
