using System.IO;
using System.Windows;
using NETpro.Labels;
using NETpro.Networking;
using NETpro.Oui;
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
        var labelStore = new JsonFileDeviceLabelStore();

        var vendorLookupTask = Task.Run(() =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "oui_vendors.tsv");
            return (IOuiVendorLookup)TsvOuiVendorLookup.LoadFromFile(path);
        });

        async Task<NetworkScanner> ScannerProvider() =>
            new(networkInfoProvider, sweeper, arpTableReader, await vendorLookupTask, pingTimeMeasurer);

        var viewModel = new MainViewModel(ScannerProvider, labelStore);
        var mainWindow = new MainWindow { DataContext = viewModel };
        mainWindow.Show();

        viewModel.RefreshCommand.Execute(null);
    }
}
