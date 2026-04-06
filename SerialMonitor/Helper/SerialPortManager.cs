
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SerialMonitor.Helper;
/// <summary>
/// Универсальный класс для работы с COM-портом
/// </summary>
public class SerialPortManager : IDisposable
{
    private SerialPort _serialPort;
    private readonly object _lockObject = new object();
    private bool _isDisposed = false;
    private string _receivedBuffer = "";
    private CancellationTokenSource _readCancellation;

    // События
    public event EventHandler<string> OnDataReceived;
    public event EventHandler<byte[]> OnBinaryDataReceived;
    public event EventHandler<string> OnError;
    public event EventHandler<bool> OnConnectionChanged;
    public event EventHandler<SerialError> OnPortError;

    // Статус соединения
    public bool IsConnected => _serialPort?.IsOpen ?? false;

    // Настройки порта
    public string PortName { get; private set; }
    public int BaudRate { get; private set; }
    public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
    public Handshake Handshake { get; set; } = Handshake.None;
    public int ReadTimeout { get; set; } = 1000;
    public int WriteTimeout { get; set; } = 1000;
    public string NewLine { get; set; } = "\r\n";

    // Режимы работы
    public bool EnableDtr { get; set; } = true;
    public bool EnableRts { get; set; } = true;
    public bool AutoNewLine { get; set; } = true; // Автоматически добавлять \r\n к командам

    /// <summary>
    /// Конструктор класса
    /// </summary>
    /// <param name="portName">Имя COM-порта (например, "COM3")</param>
    /// <param name="baudRate">Скорость передачи</param>
    public SerialPortManager(string portName, int baudRate = 9600)
    {
        PortName = portName;
        BaudRate = baudRate;
        InitializeSerialPort();
    }

    /// <summary>
    /// Инициализация COM-порта
    /// </summary>
    private void InitializeSerialPort()
    {
        if (_serialPort == null)
        {
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
    }

    /// <summary>
    /// Обновление настроек порта
    /// </summary>
    public void UpdateSettings()
    {
        bool wasConnected = IsConnected;

        if (wasConnected)
        {
            Disconnect();
        }

        _serialPort?.Dispose();
        _serialPort = null;

        InitializeSerialPort();

        if (wasConnected)
        {
            Connect();
        }
    }


    /// <summary>
    /// Подключение к COM-порту
    /// </summary>
    public bool Connect(int sleep = 100)
    {
        try
        {
            if (IsConnected)
                Disconnect();

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

    /// <summary>
    /// Отключение от COM-порта
    /// </summary>
    public void Disconnect()
    {
        try
        {
            // Остановка фонового чтения
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

    /// <summary>
    /// Обработчик получения данных
    /// </summary>
    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            // Чтение байтов для бинарного режима
            if (_serialPort.BytesToRead > 0)
            {
                byte[] buffer = new byte[_serialPort.BytesToRead];
                _serialPort.Read(buffer, 0, buffer.Length);

                // Событие для бинарных данных
                OnBinaryDataReceived?.Invoke(this, buffer);

                // Конвертируем в строку и отправляем текстовое событие
                string data = System.Text.Encoding.UTF8.GetString(buffer);
                if (!string.IsNullOrEmpty(data))
                {
                    ProcessTextData(data);
                }
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Ошибка приема данных: {ex.Message}");
        }
    }

    /// <summary>
    /// Обработка текстовых данных
    /// </summary>
    private void ProcessTextData(string data)
    {
        lock (_lockObject)
        {
            _receivedBuffer += data;

            // Разбиваем на строки
            string[] lines = _receivedBuffer.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            _receivedBuffer = lines[lines.Length - 1]; // Оставляем неполную строку

            for (int i = 0; i < lines.Length - 1; i++)
            {
                string line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    OnDataReceived?.Invoke(this, line);
                }
            }
        }
    }

    /// <summary>
    /// Отправка команды
    /// </summary>
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
                if (addNewLine && AutoNewLine)
                {
                    _serialPort.Write(command + NewLine);
                }
                else
                {
                    _serialPort.Write(command);
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, $"Ошибка отправки: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Отправка бинарных данных
    /// </summary>
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

    /// <summary>
    /// Отправка команды с ожиданием ответа
    /// </summary>
    public async Task<string> SendCommandWithResponseAsync(string command, string expectedResponse = null,
        int timeoutMs = 5000)
    {
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
        string response = "";

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

        // Таймаут
        Task timeoutTask = Task.Delay(timeoutMs);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            OnDataReceived -= handler;
            return null;
        }

        return await tcs.Task;
    }


    /// <summary>
    /// Очистка буферов
    /// </summary>
    public void ClearBuffers()
    {
        if (IsConnected)
        {
            lock (_lockObject)
            {
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _receivedBuffer = "";
            }
        }
    }

    /// <summary>
    /// Установка DTR
    /// </summary>
    public void SetDtr(bool enable)
    {
        if (IsConnected)
        {
            _serialPort.DtrEnable = enable;
        }
    }

    /// <summary>
    /// Установка RTS
    /// </summary>
    public void SetRts(bool enable)
    {
        if (IsConnected)
        {
            _serialPort.RtsEnable = enable;
        }
    }

    /// <summary>
    /// Обработчик ошибок порта
    /// </summary>
    private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        OnPortError?.Invoke(this, e.EventType);
        OnError?.Invoke(this, $"Ошибка COM-порта: {e.EventType}");
    }

    /// <summary>
    /// Освобождение ресурсов
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            Disconnect();
            _serialPort?.Dispose();
            _readCancellation?.Dispose();
            _isDisposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Получение доступных COM-портов
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }
}