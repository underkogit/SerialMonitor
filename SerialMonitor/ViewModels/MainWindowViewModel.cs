using Avalonia.Threading;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using SerialMonitor.Helper;
using SerialMonitor.Structures;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SerialMonitor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly WinDevices _winDevices;
    private SerialPortManager? _serialPortManager = null;
    private ComPortInfo? _lastComPortInfo = null;
    [ObservableProperty] private TextEditor _editor;
    [ObservableProperty] private bool _isConnected = false;

    [ObservableProperty] private string _connectButtonText = "Connect";

    public bool CanRefresh => !IsRefreshing;

    [ObservableProperty] private ObservableCollection<ComPortInfo> _ports;
    [ObservableProperty] private ComPortInfo _selectedPort;


    [ObservableProperty] private string _connectionStatus = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isRefreshing;

    [ObservableProperty] private string _receivedData = string.Empty;


    public MainWindowViewModel()
    {
        _winDevices = new WinDevices();
        _ports = [];

        _ = LoadDevicesAsync();
    }

    [RelayCommand]
    private void OnEnterPressed()
    {
        if (!string.IsNullOrWhiteSpace(ReceivedData) && _serialPortManager != null && _serialPortManager.IsConnected)
        {
            _serialPortManager.SendCommand(ReceivedData);
            AppendLine("Write", $"{ReceivedData}");
            ReceivedData = string.Empty;
        }
    }


    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task Refresh()
    {
        if (IsRefreshing) return;

        IsRefreshing = true;
        ConnectionStatus = "Refreshing device list...";
        _serialPortManager?.Dispose();
        try
        {
            await LoadDevicesAsync();
            ConnectionStatus = Ports?.Any() == true
                ? $"Devices found: {Ports.Count}"
                : "No devices found";
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Refresh error: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand()]
    private async Task Connect()
    {
        if (SelectedPort == null)
            return;
        if (_serialPortManager != null)
        {
            if (_lastComPortInfo != SelectedPort)
            {
                _serialPortManager.Dispose();
            }

            if (_serialPortManager.IsConnected)
            {
                _serialPortManager.Dispose();
                return;
            }

            _serialPortManager.Dispose();
        }


        _serialPortManager = new SerialPortManager(SelectedPort.ComPortName, 115200);
        _serialPortManager.OnConnectionChanged += async (s, connected) =>
        {
            Dispatcher.UIThread.Invoke(() => { IsConnected = connected; });

            await AppendLine("Connection Changed", $"{IsConnected}");

        };
        _serialPortManager.OnDataReceived += async (s, data) =>
        {
            await AppendLine("Read", $"Получено: {data}");

        };

        _serialPortManager.OnError += async (s, error) =>
        {

            await AppendLine("Error", $"Error: {error}");
        };


        if (_serialPortManager.Connect())
        {

            _lastComPortInfo = SelectedPort;
        }



    }

    private async Task AppendLine(string Type, string line)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Editor.Text += $"{DateTime.Now:HH:mm:ss}:[{Type}]{line}{Environment.NewLine}";

        });
    }

    private async Task LoadDevicesAsync()
    {
        if (_winDevices == null) return;

        await Task.Run(() =>
        {
            try
            {
                var devices = _winDevices.TryGetDevices();
                System.Diagnostics.Debug.WriteLine($"Loaded {devices?.Count ?? 0} devices");

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (Ports == null) return;

                    Ports.Clear();

                    if (devices?.Any() == true)
                    {
                        var validDevices = devices
                            .Where(d => d != null)
                            .OrderBy(d => d.ComPortName)
                            .ThenBy(d => d.DeviceName)
                            .ToList();

                        Ports.AddRange(validDevices);
                    }

                    if (!Ports.Any())
                    {
                        ConnectionStatus = "No devices found. Check connection.";
                    }
                });
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { ConnectionStatus = $"Load error: {ex.Message}"; });
            }
        });
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        Console.WriteLine($"Property changed: {e.PropertyName}");
    }

    partial void OnIsConnectedChanged(bool value)
    {
        ConnectButtonText = value ? "Disconnect" : "Connect";
    }

    public void Dispose()
    {
        _winDevices?.Dispose();
    }
}