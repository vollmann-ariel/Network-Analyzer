using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using NETpro.Export;
using NETpro.ViewModels;

namespace NETpro;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"netpro_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dialog.ShowDialog() == true)
        {
            // Excel only auto-detects UTF-8 (rather than misreading it as the system codepage
            // and mangling the ping/placeholder symbols) when the file starts with a BOM.
            var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            File.WriteAllText(dialog.FileName, DeviceCsvExporter.ToCsv(viewModel.Devices), utf8WithBom);
        }
    }
}