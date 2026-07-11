using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NETpro.Models;
using NETpro.Networking;

namespace NETpro.ViewModels;

public partial class MainViewModel(Func<Task<NetworkScanner>> scannerProvider) : ObservableObject
{
    public ObservableCollection<DeviceInfo> Devices { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasScanned;

    [ObservableProperty]
    private bool _showEmptyHint;

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        ShowEmptyHint = false;
        try
        {
            var scanner = await scannerProvider();
            switch (await scanner.ScanAsync())
            {
                case ScanResult.Success success:
                    Devices.Clear();
                    foreach (var device in success.Devices) Devices.Add(device);
                    ShowEmptyHint = success.Devices.Count == 1;
                    break;
                case ScanResult.NoActiveNetwork:
                    ErrorMessage = "No se detectó una red activa. Conectate a tu red e intentá de nuevo.";
                    break;
                case ScanResult.SubnetTooLarge:
                    ErrorMessage = $"Esta red es demasiado grande para escanear (más de {NetworkScanner.MaxScanHosts} direcciones posibles).";
                    break;
                case ScanResult.ArpUnavailable:
                    ErrorMessage = "No se pudo leer la tabla de vecinos IP en este equipo.";
                    break;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Ocurrió un error inesperado al escanear la red.";
        }
        finally
        {
            IsLoading = false;
            HasScanned = true;
        }
    }
}
