using System.IO;
using System.Windows;
using NETpro.Networking;
using NETpro.Oui;
using NETpro.Persistence;
using NETpro.ViewModels;

namespace NETpro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var networkInfoProvider = new SystemNetworkInfoProvider();
        var sweeper = new NetworkSweeper(new PingHostProber());
        var arpTableReader = new IpHlpApiArpTableReader();
        var pingTimeMeasurer = new IcmpPingTimeMeasurer();
        var recordStore = new JsonFileDeviceRecordStore();
        var settingsStore = new JsonFileAppSettingsStore();

        var vendorLookupTask = Task.Run(() =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "oui_vendors.tsv");
            return (IOuiVendorLookup)TsvOuiVendorLookup.LoadFromFile(path);
        });

        async Task<NetworkScanner> ScannerProvider() =>
            new(networkInfoProvider, sweeper, arpTableReader, await vendorLookupTask, pingTimeMeasurer);

        var viewModel = new MainViewModel(ScannerProvider, recordStore, settingsStore);
        var mainWindow = new MainWindow { DataContext = viewModel };
        mainWindow.Show();

        ApplyVendorLookupOnceLoadedAsync(viewModel, vendorLookupTask);
        viewModel.RefreshCommand.Execute(null);
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
