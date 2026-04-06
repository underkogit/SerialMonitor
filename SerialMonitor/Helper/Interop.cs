using System;
using System.Runtime.InteropServices;

namespace SerialMonitor.Helper;

public static class Interop
{
    private const string Setupapi = "setupapi.dll";
    public const uint DIGCF_PRESENT = 0x00000002;
    public const uint DIGCF_DEVICEINTERFACE = 0x00000010;
    public const uint SPDRP_FRIENDLYNAME = 0x0000000C;

    public static Guid GuidComPort = new Guid(
        0x86e0d1e0, 0x8089, 0x11d0,
        0x9c, 0xe4, 0x08, 0x00, 0x3e, 0x30, 0x1f, 0x73
    );


    [DllImport(Setupapi, SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(
        ref Guid ClassGuid,
        IntPtr Enumerator,
        IntPtr hwndParent,
        uint Flags
    );

    [DllImport(Setupapi, SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [DllImport(Setupapi, SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInfo(
        IntPtr DeviceInfoSet,
        uint MemberIndex,
        ref SP_DEVINFO_DATA DeviceInfoData
    );

    [DllImport(Setupapi, CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr DeviceInfoSet,
        ref SP_DEVINFO_DATA DeviceInfoData,
        uint Property,
        out uint PropertyRegDataType,
        IntPtr PropertyBuffer,
        uint PropertyBufferSize,
        out uint RequiredSize
    );

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }
}