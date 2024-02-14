using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cor64.IO
{
    public static class N64MemoryDeviceHelper
    {
        private static bool IsAligned(long address, int size) {
            return (address & (size - 1)) == 0;
        }

        private static unsafe void ReadUnaligned(nint dstPtr, N64MemoryDevice device, int deviceOffset, int size) {
            byte * _ptr = (byte *)dstPtr;

            if (size == 1) deviceOffset = N64Endianess.Address8(deviceOffset);
            if (size == 2) deviceOffset = N64Endianess.Address16(deviceOffset);

            for (int i = 0; i < size; i++) {
                *_ptr++ = device.Read8(deviceOffset++);
            }
        }

        private static unsafe void WriteUnaligned(nint srcPtr, N64MemoryDevice device, int deviceOffset, int size) {
            byte * _ptr = (byte *)srcPtr;

            if (size == 1) deviceOffset = N64Endianess.Address8(deviceOffset);
            if (size == 2) deviceOffset = N64Endianess.Address16(deviceOffset);

            for (int i = 0; i < size; i++) {
                device.Write8(deviceOffset++, *_ptr++);
            }
        }

        public static unsafe T SmartRead<T>(this N64MemoryDevice device, int deviceOffset)
        where T : unmanaged
        {
            T read = default;
            nint readPtr = (nint)(&read);

            if (IsAligned(deviceOffset, sizeof(T)))
            {
                switch (sizeof(T))
                {
                    case 1: readPtr.AsType_8(device.Read8(deviceOffset)); break;
                    case 2: readPtr.AsType_16(device.Read16(deviceOffset)); break;
                    case 4: readPtr.AsType_32(device.Read32(deviceOffset)); break;
                    case 8: readPtr.AsType_64(device.Read64(deviceOffset)); break;
                    default: ReadUnaligned(readPtr, device, deviceOffset, sizeof(T)); break;
                }
            }
            else
            {
                ReadUnaligned(readPtr, device, deviceOffset, sizeof(T));
            }

            return read;
        }

        public static unsafe void SmartWrite<T>(this N64MemoryDevice device, int deviceOffset, T value)
        where T : unmanaged
        {
            nint writePtr = (nint)(&value);

            if (IsAligned(deviceOffset, sizeof(T)))
            {
                switch (sizeof(T))
                {
                    case 1: device.Write8(deviceOffset, writePtr.AsType_8()); break;
                    case 2: device.Write16(deviceOffset, writePtr.AsType_16()); break;
                    case 4: device.Write32(deviceOffset, writePtr.AsType_32()); break;
                    case 8: device.Write64(deviceOffset, writePtr.AsType_64()); break;
                    default: WriteUnaligned(writePtr, device, deviceOffset, sizeof(T)); break;
                }
            }
            else
            {
                WriteUnaligned(writePtr, device, deviceOffset, sizeof(T));
            }
        }
    }
}