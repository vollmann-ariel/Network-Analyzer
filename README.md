# NETpro

A Windows desktop app that scans your local network and lists every device it finds — IP address, MAC address, inferred vendor, and round-trip ping latency — without requiring administrator privileges.

## Features

- **Network scan**: sweeps the local subnet and reads the OS's IP neighbor table to discover devices, no elevation required.
- **Vendor lookup**: infers the manufacturer of each device from its MAC address using the IEEE OUI registry (~40k entries, bundled locally). Randomized/locally-administered MAC addresses are flagged as such instead of showing as an unknown vendor.
- **Ping latency**: measures round-trip time per device and reports the **rolling median** over recent samples, so an occasional network spike doesn't skew the number. Devices that don't respond are marked as such in the list.
- **Custom labels**: attach a personal reference (e.g. "Living room TV") to any device; labels are keyed by MAC address and persist across scans and restarts.
- **Device history**: every device ever seen (not just labeled ones) is remembered across restarts — IP, vendor, and label — so it's still listed (as unreachable) even when it's currently offline. Unlabeled randomized-MAC devices are the one exception: their address rotates per network, so persisting them would just accumulate entries that are never seen again.
- **Auto-refresh**: optionally re-scan on a configurable interval (persisted across restarts). New devices are added as they appear; devices that stop responding are kept in the list rather than removed.
- **HTML export**: export the current device list as a self-contained HTML file, viewable in any browser.
- **Click-to-copy**: click any cell to copy its value to the clipboard.
- **Service detection**: probes each device for common open ports (HTTP, HTTPS, SSH, RDP, FTP, Telnet, SMB) with a plain TCP connect scan — no elevation required. Shown both as friendly service names and as raw port numbers.
- **New device alerts**: a Windows notification fires the first time a genuinely new device joins the network (suppressed on the app's first scan of a session, and for unlabeled randomized-MAC devices, to avoid noise).

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build and run from source

## Getting started

```powershell
dotnet build
dotnet run --project NETpro
```

## Running tests

```powershell
dotnet test
```

## Project layout

```
NETpro/                  WPF application (net10.0-windows)
  Networking/             Subnet math, host sweeping, ARP table reading, ping measurement, port scanning
  Oui/                    MAC vendor lookup (IEEE OUI database)
  Persistence/            Device history (%LocalAppData%\NETpro\devices.json) and app settings (settings.json)
  Notifications/          Windows notification for newly-seen devices
  Export/                 HTML export of the device list
  ViewModels/              MVVM view model (CommunityToolkit.Mvvm)
  Assets/oui_vendors.tsv  Generated vendor database (see scripts/generate_oui_asset.py)
NETpro.Tests/             xUnit test suite (fakes at the network/filesystem boundary)
```

## How it works

Device discovery reads Windows' IP Helper API (`GetIpNetTable2`) rather than parsing `/proc`-style files or requiring raw sockets, so it works for any unprivileged process. Vendor names come from a local snapshot of the IEEE OUI registry rather than a network call.

## Known limitations

- **Randomized MAC addresses**: modern phones (Android, iOS) often use a random, rotating MAC per network rather than their factory-assigned one. Such devices are flagged as having a randomized MAC, and a custom label on one won't survive the address changing.
- **Model inference**: only the vendor is inferred from the MAC; the exact device model isn't reliably derivable from a MAC address alone.
