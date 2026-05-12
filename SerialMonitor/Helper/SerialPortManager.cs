using System;
using System.ComponentModel.DataAnnotations;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SerialMonitor.Helper;

public class SerialPortManager : IDisposable
{
    private SerialPort _serialPort;
    private readonly object _lockObject = new();
    private bool _isDisposed = false;
    private string _receivedBuffer = "";
    private CancellationTokenSource _readCancellation;
    private int _duplications = 0;
    private string _lastLine = string.Empty;

    public event EventHandler<string> OnDataReceived;
    public event EventHandler<byte[]> OnBinaryDataReceived;
    public event EventHandler<string> OnError;
    public event EventHandler<bool> OnConnectionChanged;
    public event EventHandler<SerialError> OnPortError;

    public bool IsConnected => _serialPort?.IsOpen ?? false;

    public string PortName { get; private set; }
    [Range(0, 100)] public int MaxDuplications { get; set; } = 10;
    public int BaudRate { get; private set; }
    public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
    public Handshake Handshake { get; set; } = Handshake.None;
    public int ReadTimeout { get; set; } = 1000;
    public int WriteTimeout { get; set; } = 1000;
    public string NewLine { get; set; } = "\r\n";
    public bool EnableDtr { get; set; } = true;
    public bool EnableRts { get; set; } = true;
    public bool AutoNewLine { get; set; } = true;

    public SerialPortManager(string portName, int baudRate = 9600)
    {
        PortName = portName;
        BaudRate = baudRate;
        InitializeSerialPort();
    }

    private void InitializeSerialPort()
    {
        if (_serialPort != null) return;

        _serialPort = new SerialPort
        {
            PortName = PortName,
            BaudRate = BaudRate,
            DataBits = DataBits,
            Parity = Parity,
            StopBits = StopBits,
            Handshake = Handshake,
            ReadTimeout = ReadTimeout,
            WriteTimeout = WriteTimeout,
            DtrEnable = EnableDtr,
            RtsEnable = EnableRts,
            NewLine = NewLine
        };

        _serialPort.DataReceived += SerialPort_DataReceived;
        _serialPort.ErrorReceived += SerialPort_ErrorReceived;
    }

    public void UpdateSettings()
    {
        var wasConnected = IsConnected;
        if (wasConnected) Disconnect();

        _serialPort?.Dispose();
        _serialPort = null;
        InitializeSerialPort();

        if (wasConnected) Connect();
    }

    public bool Connect(int sleep = 100)
    {
        try
        {
            if (IsConnected) Disconnect();

            InitializeSerialPort();
            _serialPort.Open();
            Thread.Sleep(sleep);
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            _receivedBuffer = "";
            _readCancellation = new CancellationTokenSource();

            OnConnectionChanged?.Invoke(this, true);
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Ошибка подключения: {ex.Message}");
            return false;
        }
    }

    public void Disconnect()
    {
        try
        {
            _readCancellation?.Cancel();
            _readCancellation?.Dispose();
            _readCancellation = null;

            if (_serialPort?.IsOpen == true)
            {
                Thread.Sleep(50);
                _serialPort.Close();
            }

            OnConnectionChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Ошибка отключения: {ex.Message}");
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serialPort.BytesToRead <= 0) return;

            var buffer = new byte[_serialPort.BytesToRead];
            _serialPort.Read(buffer, 0, buffer.Length);

            OnBinaryDataReceived?.Invoke(this, buffer);

            var data = System.Text.Encoding.UTF8.GetString(buffer);
            if (!string.IsNullOrEmpty(data)) ProcessTextData(data);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Ошибка приема данных: {ex.Message}");
        }
    }

    private void ProcessTextData(string data)
    {
        lock (_lockObject)
        {
            _receivedBuffer += data;
            var lines = _receivedBuffer.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            _receivedBuffer = lines.Last();

            foreach (var line in lines.Take(lines.Length - 1))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                OnDataReceived?.Invoke(this, trimmedLine);

                if (trimmedLine == _lastLine) _duplications++;

                if (_duplications > MaxDuplications)
                {
                    Disconnect();
                    OnError?.Invoke(this, $"Disconnect: Maximum ({MaxDuplications}) duplicates reached");
                }

                _lastLine = trimmedLine;
            }
        }
    }

    public bool SendCommand(string command, bool addNewLine = true)
    {
        if (!IsConnected)
        {
            OnError?.Invoke(this, "Не подключено к COM-порту");
            return false;
        }

        try
        {
            lock (_lockObject)
            {
                _serialPort.Write(addNewLine && AutoNewLine ? command + NewLine : command);
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Ошибка отправки: {ex.Message}");
            return false;
        }
    }

    public bool SendBinaryData(byte[] data)
    {
        if (!IsConnected)
        {
            OnError?.Invoke(this, "Не подключено к COM-порту");
            return false;
        }

        try
        {
            lock (_lockObject)
            {
                _serialPort.Write(data, 0, data.Length);
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Ошибка отправки бинарных данных: {ex.Message}");
            return false;
        }
    }

    public async Task<string> SendCommandWithResponseAsync(string command, string expectedResponse = null,
        int timeoutMs = 5000)
    {
        var tcs = new TaskCompletionSource<string>();
        var response = "";

        EventHandler<string> handler = null;
        handler = (s, data) =>
        {
            response += data + "\n";
            if (expectedResponse == null || response.Contains(expectedResponse))
            {
                OnDataReceived -= handler;
                tcs.TrySetResult(response);
            }
        };

        OnDataReceived += handler;

        if (!SendCommand(command))
        {
            OnDataReceived -= handler;
            return null;
        }

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));

        if (completedTask == tcs.Task) return await tcs.Task;

        OnDataReceived -= handler;
        return null;
    }

    public void ClearBuffers()
    {
        if (!IsConnected) return;

        lock (_lockObject)
        {
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            _receivedBuffer = "";
        }
    }

    public void SetDtr(bool enable)
    {
        if (IsConnected) _serialPort.DtrEnable = enable;
    }

    public void SetRts(bool enable)
    {
        if (IsConnected) _serialPort.RtsEnable = enable;
    }

    private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        OnPortError?.Invoke(this, e.EventType);
        OnError?.Invoke(this, $"Ошибка COM-порта: {e.EventType}");
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        Disconnect();
        _serialPort?.Dispose();
        _readCancellation?.Dispose();
        _isDisposed = true;

        GC.SuppressFinalize(this);
    }

    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }
}