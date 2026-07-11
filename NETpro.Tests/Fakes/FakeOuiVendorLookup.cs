using NETpro.Oui;

namespace NETpro.Tests.Fakes;

public sealed class FakeOuiVendorLookup(string vendor = "Fake Vendor") : IOuiVendorLookup
{
    public string Lookup(string macAddress) => vendor;
}
