using SerialMonitor.Helper;

namespace SerialMonitor.Structures;

public class ComPortInfo
{
    public Interop.SP_DEVINFO_DATA DeviceInfoData { get; set; }
    public string DeviceName { get; set; }
    public string ComPortName { get; set; }

    public ComPortInfo(Interop.SP_DEVINFO_DATA deviceInfoData, string deviceName, string comPortName)
    {
        DeviceInfoData = deviceInfoData;
        DeviceName = deviceName ?? string.Empty;
        ComPortName = comPortName ?? string.Empty;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(ComPortName) && string.IsNullOrEmpty(DeviceName)
            ? "Unknown Device"
            : $"{ComPortName} - {DeviceName}";
    }
}