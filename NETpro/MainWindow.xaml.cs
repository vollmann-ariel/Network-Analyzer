using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using NETpro.Export;
using NETpro.ViewModels;

namespace NETpro;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _copyToastTimer = new() { Interval = TimeSpan.FromMilliseconds(1200) };

    public MainWindow()
    {
        InitializeComponent();
        _copyToastTimer.Tick += (_, _) => HideCopyToast();
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

    private void OnDeviceCellClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source) return;
        var cell = FindAncestor<DataGridCell>(source);
        var text = CellText(cell);
        if (string.IsNullOrEmpty(text)) return;
        Clipboard.SetText(text);
        ShowCopyToast(text);
    }

    private void ShowCopyToast(string value)
    {
        CopyToastText.Text = $"Copiado: {value}";
        CopyToast.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(120)));
        _copyToastTimer.Stop();
        _copyToastTimer.Start();
    }

    private void HideCopyToast()
    {
        _copyToastTimer.Stop();
        CopyToast.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)));
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private static string? CellText(DataGridCell? cell) => cell?.Content switch
    {
        TextBlock textBlock => textBlock.Text,
        TextBox textBox => textBox.Text,
        _ => null
    };
}