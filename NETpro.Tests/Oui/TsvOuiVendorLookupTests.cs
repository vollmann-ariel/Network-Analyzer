using NETpro.Oui;

namespace NETpro.Tests.Oui;

public class TsvOuiVendorLookupTests
{
    private static readonly TsvOuiVendorLookup Lookup = new(new Dictionary<string, string>
    {
        ["F4F5D8"] = "Google, Inc."
    });

    [Fact]
    public void Lookup_ReturnsVendor_ForKnownPrefix()
    {
        Assert.Equal("Google, Inc.", Lookup.Lookup("F4:F5:D8:11:22:33"));
    }

    [Fact]
    public void Lookup_NormalizesColonSeparatedLowercaseMac()
    {
        Assert.Equal("Google, Inc.", Lookup.Lookup("f4:f5:d8:aa:bb:cc"));
    }

    [Fact]
    public void Lookup_ReturnsDesconocido_ForUnknownPrefix()
    {
        Assert.Equal(TsvOuiVendorLookup.UnknownVendor, Lookup.Lookup("00:00:00:11:22:33"));
    }

    [Fact]
    public void Lookup_ReturnsDesconocido_ForMalformedShortMac()
    {
        Assert.Equal(TsvOuiVendorLookup.UnknownVendor, Lookup.Lookup("AA:BB"));
    }

    [Fact]
    public void LoadFromFile_ParsesRealTsvFormat()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample_oui.tsv");
        var loaded = TsvOuiVendorLookup.LoadFromFile(path);
        Assert.Equal("Google, Inc.", loaded.Lookup("F4:F5:D8:00:00:00"));
    }
}
