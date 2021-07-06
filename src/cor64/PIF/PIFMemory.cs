using System;
using cor64.IO;
using NLog;

namespace cor64.PIF
{
    public class PIFMemory : PerpherialDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MemMappedBuffer m_Rom = new MemMappedBuffer(0x7C0, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private readonly MemMappedBuffer m_Ram = new MemMappedBuffer(0x40, MemMappedBuffer.MemModel.SINGLE_READ_WRITE);

        public const uint ADDRESS_JOY_CHANNEL = 0x1FC007C4U;

        public PIFMemory(N64MemoryController n64MemoryController) : base(n64MemoryController, 0x100000)
        {
            Map(m_Rom, m_Ram);
        }

        public void JoyWriteButtons(int slot, JoyController.ButtonPress buttonPress) {
            // Log.Debug("Joy Button Press: {0}", buttonPress.ToString());

            #if LITTLE_ENDIAN
            m_Ram.WritePtr.Offset(4).AsType_16((ushort)buttonPress);
            #else
            m_Ram.WritePtr.Offset(4).AsType_16Swp((ushort)buttonPress);
            #endif
        }

        // public void ClearJoy() {
        //     m_Ram.WritePtr.Offset(4).AsType_16(0);
        // }

        // public uint ReadPifValue(int index) {
        //     return m_Ram.ReadPtr.Offset(index * 4).AsType_32Swp();
        // }
    }
}
