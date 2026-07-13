using System.ComponentModel;
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
using NETpro.Persistence;
using NETpro.ViewModels;

namespace NETpro;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IAppSettingsStore _settingsStore;
    private readonly DispatcherTimer _copyToastTimer = new() { Interval = TimeSpan.FromMilliseconds(1200) };
    private string? _pendingCopyText;

    public MainWindow(IAppSettingsStore settingsStore)
    {
        InitializeComponent();
        _settingsStore = settingsStore;
        _copyToastTimer.Tick += (_, _) => HideCopyToast();
        ApplySavedWindowState();
        Closing += OnWindowClosing;
    }

    private void ApplySavedWindowState()
    {
        var settings = _settingsStore.Load();
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;
        foreach (var column in DevicesGrid.Columns)
        {
            if (column.Header?.ToString() is { } header && settings.ColumnWidths.TryGetValue(header, out var width))
            {
                column.Width = new DataGridLength(width);
            }
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        var columnWidths = DevicesGrid.Columns
            .Where(c => c.Header is not null)
            .ToDictionary(c => c.Header.ToString()!, c => c.ActualWidth);
        _settingsStore.Save(_settingsStore.Load() with
        {
            WindowWidth = Width,
            WindowHeight = Height,
            ColumnWidths = columnWidths
        });
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

    private void OnDeviceCellRightClick(object sender, MouseButtonEventArgs e)
    {
        _pendingCopyText = e.OriginalSource is DependencyObject source
            ? CellText(FindAncestor<DataGridCell>(source))
            : null;
    }

    private void OnCopyMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_pendingCopyText)) return;
        Clipboard.SetText(_pendingCopyText);
        ShowCopyToast(_pendingCopyText);
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