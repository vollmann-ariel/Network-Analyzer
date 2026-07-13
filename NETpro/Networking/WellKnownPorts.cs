namespace NETpro.Networking;

public static class WellKnownPorts
{
    public static readonly IReadOnlyDictionary<int, string> Names = new Dictionary<int, string>
    {
        [21] = "FTP",
        [22] = "SSH",
        [23] = "Telnet",
        [80] = "HTTP",
        [443] = "HTTPS",
        [445] = "SMB",
        [3389] = "RDP",
        [8080] = "HTTP",
    };
}
