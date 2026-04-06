using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SerialMonitor.Structures;

namespace SerialMonitor.Helper;

public class WinDevices
{
    private readonly Regex _regex = new Regex(@"\((COM\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private IntPtr _deviceInfoSet;
    private bool _disposed;

    public WinDevices()
    {
        // Инициализируем список устройств при создании объекта
        RefreshDeviceList();
    }

    public void RefreshDeviceList()
    {
        // Уничтожаем старый список, если он существует
        if (_deviceInfoSet != IntPtr.Zero && _deviceInfoSet != new IntPtr(-1))
        {
            Interop.SetupDiDestroyDeviceInfoList(_deviceInfoSet);
        }

        // Создаем новый список устройств
        _deviceInfoSet = Interop.SetupDiGetClassDevs(
            ref Interop.GuidComPort,
            IntPtr.Zero,
            IntPtr.Zero,
            Interop.DIGCF_PRESENT | Interop.DIGCF_DEVICEINTERFACE
        );
    }

    public ObservableCollection<ComPortInfo> TryGetDevices()
    {
        ObservableCollection<ComPortInfo> devices = new ObservableCollection<ComPortInfo>();

        if (_deviceInfoSet == IntPtr.Zero || _deviceInfoSet == new IntPtr(-1))
        {
            Console.WriteLine("Failed to get device list");
            return devices;
        }

        try
        {
            Interop.SP_DEVINFO_DATA deviceInfoData = new Interop.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = (uint)Marshal.SizeOf(typeof(Interop.SP_DEVINFO_DATA));

            for (uint i = 0; Interop.SetupDiEnumDeviceInfo(_deviceInfoSet, i, ref deviceInfoData); i++)
            {
                string friendlyName = GetDeviceProperty(_deviceInfoSet, ref deviceInfoData, Interop.SPDRP_FRIENDLYNAME);

                if (!string.IsNullOrEmpty(friendlyName))
                {
                    var match = _regex.Match(friendlyName);
                    if (match.Success)
                    {
                        string comPort = match.Groups[1].Value;

                        devices.Add(new ComPortInfo(
                            deviceInfoData,
                            friendlyName,
                            comPort
                        ));

                        Console.WriteLine($"Found device: {comPort} - {friendlyName}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enumerating devices: {ex.Message}");
        }

        return devices;
    }

    private string GetDeviceProperty(IntPtr deviceInfoSet, ref Interop.SP_DEVINFO_DATA deviceInfoData, uint property)
    {
        uint requiredSize = 0;
        uint regType = 0;

        Interop.SetupDiGetDeviceRegistryProperty(
            deviceInfoSet, ref deviceInfoData, property,
            out regType, IntPtr.Zero, 0, out requiredSize
        );

        if (requiredSize <= 0) return null;

        IntPtr buffer = IntPtr.Zero;
        try
        {
            buffer = Marshal.AllocHGlobal((int)requiredSize);
            if (Interop.SetupDiGetDeviceRegistryProperty(
                    deviceInfoSet, ref deviceInfoData, property,
                    out regType, buffer, requiredSize, out requiredSize))
            {
                return Marshal.PtrToStringAuto(buffer);
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
        }

        return null;
    }

    public void Dispose()
    {
        if (!_disposed && _deviceInfoSet != IntPtr.Zero && _deviceInfoSet != new IntPtr(-1))
        {
            Interop.SetupDiDestroyDeviceInfoList(_deviceInfoSet);
            _disposed = true;
        }
    }
}