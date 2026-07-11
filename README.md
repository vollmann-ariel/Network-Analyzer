# NETpro

A Windows desktop app that scans your local network and lists every device it finds — IP address, MAC address, inferred vendor, and round-trip ping latency — without requiring administrator privileges.

## Features

- **Network scan**: sweeps the local subnet and reads the OS's IP neighbor table to discover devices, no elevation required.
- **Vendor lookup**: infers the manufacturer of each device from its MAC address using the IEEE OUI registry (~40k entries, bundled locally).
- **Ping latency**: measures round-trip time per device and reports the **rolling median** over recent samples, so an occasional network spike doesn't skew the number.
- **Custom labels**: attach a personal reference (e.g. "Living room TV") to any device; labels are keyed by MAC address and persist across scans and restarts.
- **Auto-refresh**: optionally re-scan on a configurable interval. New devices are added as they appear; devices that stop responding are kept in the list rather than removed.

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
  Networking/             Subnet math, host sweeping, ARP table reading, ping measurement
  Oui/                    MAC vendor lookup (IEEE OUI database)
  Labels/                 Persisted per-device labels (%LocalAppData%\NETpro\labels.json)
  ViewModels/              MVVM view model (CommunityToolkit.Mvvm)
  Assets/oui_vendors.tsv  Generated vendor database (see scripts/generate_oui_asset.py)
NETpro.Tests/             xUnit test suite (fakes at the network/filesystem boundary)
```

## How it works

Device discovery reads Windows' IP Helper API (`GetIpNetTable2`) rather than parsing `/proc`-style files or requiring raw sockets, so it works for any unprivileged process. Vendor names come from a local snapshot of the IEEE OUI registry rather than a network call.

## Known limitations

- **Randomized MAC addresses**: modern phones (Android, iOS) often use a random, rotating MAC per network rather than their factory-assigned one. Such devices show up as vendor "Unknown," and their custom label won't survive the MAC changing.
- **Model inference**: only the vendor is inferred from the MAC; the exact device model isn't reliably derivable from a MAC address alone.
