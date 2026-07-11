using System.IO;

namespace NETpro.Oui;

public interface IOuiVendorLookup
{
    string Lookup(string macAddress);
}

public sealed class TsvOuiVendorLookup(IReadOnlyDictionary<string, string> vendorsByPrefix) : IOuiVendorLookup
{
    public const string UnknownVendor = "Desconocido";
    public const string RandomizedMacVendor = "MAC aleatoria/privada";

    public string Lookup(string macAddress)
    {
        if (IsLocallyAdministered(macAddress)) return RandomizedMacVendor;
        var prefix = NormalizePrefix(macAddress);
        return prefix is not null && vendorsByPrefix.TryGetValue(prefix, out var vendor) ? vendor : UnknownVendor;
    }

    public static string? NormalizePrefix(string mac)
    {
        var hex = new string(mac.Where(c => c is not (':' or '-')).ToArray()).ToUpperInvariant();
        return hex.Length >= 6 ? hex[..6] : null;
    }

    /// <summary>
    /// The 2nd-least-significant bit of a MAC's first octet marks it as locally administered
    /// (set by the OS/driver, e.g. Android/iOS per-network MAC randomization) rather than
    /// factory-assigned by the manufacturer — such addresses never appear in the OUI registry.
    /// </summary>
    public static bool IsLocallyAdministered(string mac)
    {
        var prefix = NormalizePrefix(mac);
        return prefix is not null && (Convert.ToByte(prefix[..2], 16) & 0x02) != 0;
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
