using System.IO;
using System.Windows;
using NETpro.Networking;
using NETpro.Notifications;
using NETpro.Oui;
using NETpro.Persistence;
using NETpro.ViewModels;

namespace NETpro;

public partial class App : Application
{
    private TrayDeviceNotifier? _deviceNotifier;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var networkInfoProvider = new SystemNetworkInfoProvider();
        var sweeper = new NetworkSweeper(new PingHostProber());
        var arpTableReader = new IpHlpApiArpTableReader();
        var pingTimeMeasurer = new IcmpPingTimeMeasurer();
        var portScanner = new TcpPortScanner();
        var recordStore = new JsonFileDeviceRecordStore();
        var settingsStore = new JsonFileAppSettingsStore();
        _deviceNotifier = new TrayDeviceNotifier();

        var vendorLookupTask = Task.Run(() =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "oui_vendors.tsv");
            return (IOuiVendorLookup)TsvOuiVendorLookup.LoadFromFile(path);
        });

        async Task<NetworkScanner> ScannerProvider() =>
            new(networkInfoProvider, sweeper, arpTableReader, await vendorLookupTask, pingTimeMeasurer, portScanner);

        var viewModel = new MainViewModel(ScannerProvider, recordStore, settingsStore, _deviceNotifier);
        var mainWindow = new MainWindow { DataContext = viewModel };
        mainWindow.Show();

        ApplyVendorLookupOnceLoadedAsync(viewModel, vendorLookupTask);
        viewModel.RefreshCommand.Execute(null);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _deviceNotifier?.Dispose();
        base.OnExit(e);
    }

    private static async void ApplyVendorLookupOnceLoadedAsync(MainViewModel viewModel, Task<IOuiVendorLookup> vendorLookupTask)
    {
        try
        {
            viewModel.ApplyVendorLookup(await vendorLookupTask);
        }
        catch
        {
            // Same failure the initial scan already surfaces via ErrorMessage; nothing more to do here.
        }
    }
}
