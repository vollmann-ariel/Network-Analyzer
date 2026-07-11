using System.IO;

namespace NETpro.Oui;

public interface IOuiVendorLookup
{
    string Lookup(string macAddress);
}

public sealed class TsvOuiVendorLookup(IReadOnlyDictionary<string, string> vendorsByPrefix) : IOuiVendorLookup
{
    public const string UnknownVendor = "Desconocido";

    public string Lookup(string macAddress)
    {
        var prefix = NormalizePrefix(macAddress);
        return prefix is not null && vendorsByPrefix.TryGetValue(prefix, out var vendor) ? vendor : UnknownVendor;
    }

    public static string? NormalizePrefix(string mac)
    {
        var hex = new string(mac.Where(c => c is not (':' or '-')).ToArray()).ToUpperInvariant();
        return hex.Length >= 6 ? hex[..6] : null;
    }

    public static TsvOuiVendorLookup LoadFromFile(string path)
    {
        var map = new Dictionary<string, string>(40_000, StringComparer.Ordinal);
        foreach (var line in File.ReadLines(path))
        {
            var tab = line.IndexOf('\t');
            if (tab > 0) map[line[..tab]] = line[(tab + 1)..];
        }
        return new TsvOuiVendorLookup(map);
    }
}
