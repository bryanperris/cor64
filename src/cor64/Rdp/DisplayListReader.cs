using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using cor64.IO;
using NLog;

namespace cor64.Rdp {
    public class DisplayListReader {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly N64MemoryController m_PhysicalMemory;
        

        /// <summary>
        /// Reads Display Lists from big-endian memory sources
        /// </summary>
        /// <param name="source"></param>
        public DisplayListReader(N64MemoryController memory) {
            m_PhysicalMemory = memory;
        }

        public IReadOnlyList<RdpCommand> ReadDisplayList(long address, int size) {
            List<RdpCommand> commands = new();
            int count = 0;

            while (count < size) {
                /* Read the command type */
                /* Always mask out the upper 2 unused bits (in case its signed) */

                int commandId = m_PhysicalMemory.U8(address) & 0x3F;

                // Log.Debug("RDP Command ID: " + commandId.ToString("X"));

                RdpCommandType type = DisplayListDecoder.Decode(commandId);

                byte[] data = new byte[type.Size];

                // The RDP command class expects the command in reverse order
                for (int i = 0; i < data.Length; i++) {
                    long addr = (address + i) ^ 7;
                    data[i] = m_PhysicalMemory.U8(addr);
                }

                commands.Add(new RdpCommand(type, data));

                count += data.Length;
                address += type.Size;
            }

            return commands;
        }
    }
}