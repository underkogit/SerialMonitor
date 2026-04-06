
#include "serialib.h"
#include <cstring>

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

extern "C"
{

    EXPORT void *CreateSerial()
    {
        return new serialib();
    }

    EXPORT void DestroySerial(void *serial)
    {
        delete static_cast<serialib *>(serial);
    }

    EXPORT char OpenDevice(void *serial, const char *device, unsigned int bauds,
                           int databits, int parity, int stopbits)
    {
        return static_cast<serialib *>(serial)->openDevice(
            device, bauds,
            static_cast<SerialDataBits>(databits),
            static_cast<SerialParity>(parity),
            static_cast<SerialStopBits>(stopbits));
    }

    EXPORT void CloseDevice(void *serial)
    {
        static_cast<serialib *>(serial)->closeDevice();
    }

    EXPORT bool IsDeviceOpen(void *serial)
    {
        return static_cast<serialib *>(serial)->isDeviceOpen();
    }

    EXPORT int WriteChar(void *serial, char byte)
    {
        return static_cast<serialib *>(serial)->writeChar(byte);
    }

    EXPORT int WriteString(void *serial, const char *str)
    {
        return static_cast<serialib *>(serial)->writeString(str);
    }

    EXPORT int WriteBytes(void *serial, const unsigned char *buffer, unsigned int nbBytes)
    {
        return static_cast<serialib *>(serial)->writeBytes(buffer, nbBytes);
    }

    EXPORT int ReadChar(void *serial, char *pByte, unsigned int timeoutMs)
    {
        return static_cast<serialib *>(serial)->readChar(pByte, timeoutMs);
    }

    EXPORT int ReadString(void *serial, char *buffer, char finalChar,
                          unsigned int maxNbBytes, unsigned int timeoutMs)
    {
        return static_cast<serialib *>(serial)->readString(buffer, finalChar, maxNbBytes, timeoutMs);
    }

    EXPORT int ReadBytes(void *serial, unsigned char *buffer, unsigned int maxNbBytes,
                         unsigned int timeoutMs, unsigned int sleepDurationUs)
    {
        return static_cast<serialib *>(serial)->readBytes(buffer, maxNbBytes, timeoutMs, sleepDurationUs);
    }

    EXPORT char FlushReceiver(void *serial)
    {
        return static_cast<serialib *>(serial)->flushReceiver();
    }

    EXPORT int Available(void *serial)
    {
        return static_cast<serialib *>(serial)->available();
    }

    EXPORT bool SetDTR(void *serial, bool status)
    {
        return static_cast<serialib *>(serial)->DTR(status);
    }

    EXPORT bool SetRTS(void *serial, bool status)
    {
        return static_cast<serialib *>(serial)->RTS(status);
    }

    EXPORT bool IsCTS(void *serial)
    {
        return static_cast<serialib *>(serial)->isCTS();
    }

    EXPORT bool IsDSR(void *serial)
    {
        return static_cast<serialib *>(serial)->isDSR();
    }

    EXPORT bool IsDCD(void *serial)
    {
        return static_cast<serialib *>(serial)->isDCD();
    }

    EXPORT bool IsRI(void *serial)
    {
        return static_cast<serialib *>(serial)->isRI();
    }
}