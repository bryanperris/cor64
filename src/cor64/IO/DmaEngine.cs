using System.Text;
using System.IO;
using System;
using cor64.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace cor64.IO
{
    public class DmaEngine
    {
        private readonly static string s_LogBasePath;
        private N64MemoryDevice m_DramMemory;
        private N64MemoryDevice m_RcpMemory;
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private readonly string m_Name;
        private int m_Count;
        private readonly List<byte[]> m_CapturedBlocks = new();
        private bool m_IsMonitoring;
        private uint m_MonitorSrc;
        private uint m_MonitorDst;
        private int m_MonitorLength;
        private bool m_MonitorFromDram;

        private readonly static char[] hexChars = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'A', 'B', 'C', 'D', 'E', 'F'
        };

        static DmaEngine()
        {
            s_LogBasePath = Path.Combine(Environment.CurrentDirectory, "DmaDumps");

            if (Directory.Exists(s_LogBasePath))
                Directory.Delete(s_LogBasePath, true);

            Directory.CreateDirectory(s_LogBasePath);
        }

        public DmaEngine(string name)
        {
            m_Name = name;
        }

        public void AttachMemory(N64MemoryDevice dramMemory, N64MemoryDevice rcpMemory)
        {
            m_DramMemory = dramMemory;
            m_RcpMemory = rcpMemory;
        }

        private static void ReadMemoryUnaligned(N64MemoryDevice memory, int offset, byte[] buffer, int index, int count)
        {
            for (int i = index; i < count; i++)
            {
                if (offset < memory.Size)
                {
                    buffer[i] = memory.Read8(offset);
                }
                else
                {
                    buffer[i] = 0;
                }

                offset++;
            }
        }

        private static void ReadMemory(N64MemoryDevice memory, int offset, byte[] buffer, int index, int count)
        {
            if ((offset % 4) != 0 || (count % 4) != 0)
            {
                ReadMemoryUnaligned(memory, offset, buffer, index, count);
            }
            else
            {
                unsafe {
                    fixed (byte * _ptr = &buffer[0]) {
                        uint * bufferPtr = (uint *)_ptr;

                        for (int i = index / 4; i < count / 4; i++)
                        {
                            if (offset < memory.Size)
                            {
                                *bufferPtr = memory.Read32(offset);
                            }
                            else
                            {
                                *bufferPtr = 0;
                            }

                            offset += 4;
                            bufferPtr++;
                        }
                    }
                }
            }

#if DEBUG_DMA_HEX
            m_CapturedBlocks.Add(buffer);
#endif
        }

        public void ReadRcp(int srcOffset, byte[] buffer, int index, int count)
        {
            ReadMemory(m_RcpMemory, srcOffset, buffer, index, count);
        }

        public void ReadDram(int srcOffset, byte[] buffer, int index, int count)
        {
            ReadMemory(m_DramMemory, srcOffset, buffer, index, count);
        }

        private static void WriteMemoryUnaligned(N64MemoryDevice memory, int offset, byte[] buffer, int index, int count)
        {
            for (int i = index; i < count; i++)
            {
                if (offset < memory.Size)
                {
                    memory.Write8(offset, buffer[i]);
                }
                else {
                    break;
                }

                offset++;
            }
        }

        private static void WriteMemory(N64MemoryDevice memory, int offset, byte[] buffer, int index, int count) {
            if ((offset % 4) != 0 || (count % 4) != 0)
            {
                WriteMemoryUnaligned(memory, offset, buffer, index, count);
                return;
            }

            unsafe {
                fixed (byte * _ptr = &buffer[0]) {
                    uint * bufferPtr = (uint *)_ptr;

                    for (int i = index / 4; i < count / 4; i++)
                    {
                        if (offset < memory.Size)
                        {
                            memory.Write32(offset, *bufferPtr);
                        }
                        else {
                            break;
                        }

                        offset += 4;
                        bufferPtr++;
                    }
                }
            }
        }

        public void WriteDram(int dstOffset, byte[] buffer, int index, int count)
        {
            WriteMemory(m_DramMemory, dstOffset, buffer, index, count);
        }

        public void WriteRcp(int dstOffset, byte[] buffer, int index, int count) {
            WriteMemory(m_RcpMemory, dstOffset, buffer, index, count);
        }

        public void DirectCopy_RcpToDram(int srcOffset, int dstOffset, int count) {
            for (int i = 0; i < count; i++) {
                byte read = m_RcpMemory.Read8(srcOffset + i);
                m_DramMemory.Write8(dstOffset + i, read);
            }
        }

        public void DirectCopy_DramToRcp(int srcOffset, int dstOffset, int count) {
            for (int i = 0; i < count; i++) {
                byte read = m_DramMemory.Read8(srcOffset + i);
                m_RcpMemory.Write8(dstOffset + i, read);
            }
        }

        protected void DumpTransferToFile(bool fromDram, long srcOffset, long dstOffset, byte[] buffer)
        {
            string title = fromDram ?
                String.Format("{0}-{1}-DRAM-{2:X8}-{3:X8}-{4}", m_Name, m_Count++, srcOffset, dstOffset, buffer.Length) :
                String.Format("{0}-{1}-RCP-{2:X8}-{3:X8}-{4}", m_Name, m_Count++, srcOffset, dstOffset, buffer.Length);

            string logfile = Path.Combine(s_LogBasePath, title + ".bin");

            File.WriteAllBytes(logfile, buffer);
        }

        [Conditional("DEBUG_DMA_CMDS")]
        public void StartMonitoring(bool fromDram, uint source, uint dest, int length)
        {
#if DEBUG_DMA_HEX
            m_CapturedBlocks.Clear();
#endif

            m_IsMonitoring = true;
            m_MonitorFromDram = fromDram;
            m_MonitorSrc = source;
            m_MonitorDst = dest;
            m_MonitorLength = length;
        }

        [Conditional("DEBUG_DMA_CMDS")]
        public void StopMonitoring()
        {
            if (m_IsMonitoring)
            {
                m_IsMonitoring = false;

                if (m_MonitorFromDram)
                {
                    Log.Debug("{0} DMA FROM DRAM: {1:X8} {2:X8} {3}", m_Name, m_MonitorSrc, m_MonitorDst, m_MonitorLength);
                }
                else
                {
                    Log.Debug("{0} DMA TO DRAM: {1:X8} {2:X8} {3}", m_Name, m_MonitorSrc, m_MonitorDst, m_MonitorLength);
                }

#if DEBUG_DMA_HEX

                int bufferSize = 0;

                foreach (var buffer in m_CapturedBlocks) {
                    bufferSize += bufferSize;
                }

                byte[] transferred = new byte[bufferSize];
                int index = 0;

                foreach (var buffer in m_CapturedBlocks) {
                    Array.Copy(buffer, 0, transferred, index, buffer.Length);
                    index += buffer.Length;
                }

                m_CapturedBlocks.Clear();

                DumpTransferToFile(m_MonitorFromDram, m_MonitorSrc, m_MonitorDst, transferred);
#endif
            }
        }
    }
}