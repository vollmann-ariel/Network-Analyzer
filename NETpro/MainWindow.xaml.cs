using System.IO;
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

    private void OnExportHtmlClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;
        var dialog = new SaveFileDialog
        {
            Filter = "HTML (*.html)|*.html",
            FileName = $"netpro_{DateTime.Now:yyyyMMdd_HHmmss}.html"
        };
        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName, DeviceHtmlExporter.ToHtml(viewModel.Devices));
        }
    }
}