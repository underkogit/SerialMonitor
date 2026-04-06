// serialib_wrapper_debug.cpp
#include "serialib.h"
#include <cstring>
#include <fstream>
#include <iostream>
#include <chrono>
#include <thread>

#ifdef _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

// Логирование в файл
void LogToFile(const char *message)
{
    std::ofstream log("serial_debug.log", std::ios::app);
    auto now = std::chrono::system_clock::now();
    auto time = std::chrono::system_clock::to_time_t(now);
    log << ctime(&time) << ": " << message << std::endl;
    log.close();
}

extern "C"
{

    EXPORT void *CreateSerial()
    {
        LogToFile("CreateSerial called");
        return new serialib();
    }

    EXPORT void DestroySerial(void *serial)
    {
        LogToFile("DestroySerial called");
        delete static_cast<serialib *>(serial);
    }

    EXPORT int OpenDevice(void *serial, const char *device, int bauds,
                          int databits, int parity, int stopbits)
    {
        LogToFile("OpenDevice called");

        char logMsg[512];
        sprintf(logMsg, "  Device: %s, Baud: %d, DataBits: %d, Parity: %d, StopBits: %d",
                device, bauds, databits, parity, stopbits);
        LogToFile(logMsg);

        serialib *s = static_cast<serialib *>(serial);
        int result = s->openDevice(device, bauds,
                                   static_cast<SerialDataBits>(databits),
                                   static_cast<SerialParity>(parity),
                                   static_cast<SerialStopBits>(stopbits));

        sprintf(logMsg, "  Result: %d", result);
        LogToFile(logMsg);
        return result;
    }

    EXPORT void CloseDevice(void *serial)
    {
        LogToFile("CloseDevice called");
        static_cast<serialib *>(serial)->closeDevice();
    }

    EXPORT int WriteString(void *serial, const char *str)
    {
        LogToFile("WriteString called");
        char logMsg[512];
        sprintf(logMsg, "  Sending: %s", str);
        LogToFile(logMsg);

        int result = static_cast<serialib *>(serial)->writeString(str);

        sprintf(logMsg, "  Write result: %d", result);
        LogToFile(logMsg);
        return result;
    }

    EXPORT int ReadString(void *serial, char *buffer, char finalChar,
                          int maxNbBytes, int timeOut_ms)
    {
        LogToFile("ReadString called");
        char logMsg[512];
        sprintf(logMsg, "  FinalChar: '%c', MaxBytes: %d, Timeout: %d",
                finalChar, maxNbBytes, timeOut_ms);
        LogToFile(logMsg);

        int result = static_cast<serialib *>(serial)->readString(buffer, finalChar, maxNbBytes, timeOut_ms);

        if (result > 0)
        {
            sprintf(logMsg, "  Read %d bytes: %s", result, buffer);
            LogToFile(logMsg);
        }
        else
        {
            sprintf(logMsg, "  Read result: %d", result);
            LogToFile(logMsg);
        }
        return result;
    }

    EXPORT int ReadBytes(void *serial, unsigned char *buffer, int maxNbBytes,
                         int timeOut_ms, int sleepDuration_us)
    {
        LogToFile("ReadBytes called");
        char logMsg[512];
        sprintf(logMsg, "  MaxBytes: %d, Timeout: %d", maxNbBytes, timeOut_ms);
        LogToFile(logMsg);

        int result = static_cast<serialib *>(serial)->readBytes(buffer, maxNbBytes, timeOut_ms, sleepDuration_us);

        sprintf(logMsg, "  Bytes read: %d", result);
        LogToFile(logMsg);
        return result;
    }

    EXPORT int Available(void *serial)
    {
        return static_cast<serialib *>(serial)->available();
    }

} // extern "C"