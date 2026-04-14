using Avalonia.Threading;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using SerialMonitor.Enumes;
using SerialMonitor.Helper;
using SerialMonitor.Services;
using SerialMonitor.Structures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;


namespace SerialMonitor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly WinDevices _winDevices;
    private WebSocketServer _socketServer;
    private SerialPortManager? _serialPortManager = null;
    private ComPortInfo? _lastComPortInfo = null;
    private readonly FileStorageService _fileStorageService = new FileStorageService();
    [ObservableProperty] private TextEditor? _editor;
    [ObservableProperty] private bool _isConnected = false;
    [ObservableProperty] private bool _isLogPanelVisible = false;


    [ObservableProperty] private string _connectButtonText = "Connect";

    public bool CanRefresh => !IsRefreshing;

    [ObservableProperty] private ObservableCollection<ComPortInfo> _ports;
    [ObservableProperty] private ObservableCollection<string> _listCommands = [];
    [ObservableProperty] private ComPortInfo? _selectedPort;


    [ObservableProperty] private string _connectionStatus = string.Empty;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isRefreshing;

    [ObservableProperty] private string _receivedData = string.Empty;

    [ObservableProperty] private BaudRate _selectedBaudRate = BaudRate.CBR_115200;

    public IEnumerable<BaudRate> BaudRateValues =>
        (BaudRate[])Enum.GetValues(typeof(BaudRate));


    public MainWindowViewModel()
    {
        _winDevices = new WinDevices();
        _socketServer = new WebSocketServer();

        _socketServer.MessageReceived +=  (sender, e) =>
        {
             AppendLine("TCP Write", $"{e.Message}");
        };

        _socketServer.ClientConnected += async (sender, e) =>
        {
            // Console.WriteLine($"Клиент подключен. Всего клиентов: {_socketServer.GetConnectedClientsCount()}");
            // await AppendLine("TCP Write", $"{e.Message}");
            // await _socketServer.SendToAllAsync(
            //     $"Новый клиент подключился! Всего клиентов: {_socketServer.GetConnectedClientsCount()}");
        };

        _ports = [];
        ListCommands = _fileStorageService.Load();
        _ = LoadDevicesAsync();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _socketServer.StartAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void SaveListCommands()
    {
        _fileStorageService.Save(ListCommands);
    }

    [RelayCommand]
    private void OnEnterPressed()
    {
        if (!string.IsNullOrWhiteSpace(ReceivedData) && _serialPortManager != null && _serialPortManager.IsConnected)
        {
            
            AppendLine("Write", $"{ReceivedData}", (data) =>
            {
                ListCommands.Add(data);

                SaveListCommands();
            });


            ReceivedData = string.Empty;
        }
    }

    public void SendMessageComPort(string? message)
    {
        if (_serialPortManager != null && _serialPortManager.IsConnected && !string.IsNullOrEmpty(message))
            _serialPortManager?.SendCommand(message);
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


        _serialPortManager = new SerialPortManager(SelectedPort.ComPortName, (int)SelectedBaudRate);
        _serialPortManager.MaxDuplications = 10;
        _serialPortManager.OnConnectionChanged += async (s, connected) =>
        {
            Dispatcher.UIThread.Invoke(() => { IsConnected = connected; });

            await AppendLine("Connection Changed", $"{IsConnected}");
        };
        _serialPortManager.OnDataReceived += async (s, data) =>
        {
            await AppendLine("Read", $"{data}");
        };

        _serialPortManager.OnError += async (s, error) => { await AppendLine("Error", $"{error}"); };


        if (_serialPortManager.Connect())
        {
            _lastComPortInfo = SelectedPort;
        }
    }

    private async Task AppendLine(string type, string line, Action<string>? com = null)
    {
        string time = $"{DateTime.Now:HH:mm:ss}";
        while (true)
        {
            if (time.Length > 10)
                break;
            time += " ";
        }

        string lineContent = $"{time}   [{type}]   {line}{Environment.NewLine}";
        Console.WriteLine($"Клиент подключен. Всего клиентов: {_socketServer.GetConnectedClientsCount()}");
         
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (Editor != null) Editor.Text += lineContent;
            com?.Invoke(line);
        });
    }


    private async Task LoadDevicesAsync()
    {
        if (_winDevices == null) return;

        await Task.Run(() =>
        {
            try
            {
                ObservableCollection<ComPortInfo>? devices = _winDevices.TryGetDevices();
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

    partial void OnSelectedPortChanged(ComPortInfo value)
    {
        if (SelectedPort == null)
            return;
        if (_serialPortManager != null)
        {
            _serialPortManager.Dispose();
        }
    }

    partial void OnIsConnectedChanged(bool value)
    {
        ConnectButtonText = value ? "Disconnect" : "Connect";
    }

    public void Dispose()
    {
        _winDevices?.Dispose();
        _socketServer?.Dispose();
    }
}