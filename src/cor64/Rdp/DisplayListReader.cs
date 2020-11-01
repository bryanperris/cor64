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
        private Stream m_Stream;
        

        /// <summary>
        /// Reads Display Lists from big-endian memory sources
        /// </summary>
        /// <param name="source"></param>
        public DisplayListReader(Stream source) {
            m_Stream = source;
        }

        public IReadOnlyList<RdpCommand> ReadDisplayList(long address, int size, bool xbus = false) {

            if (!xbus) {
                // RDRAM
                m_Stream.Position = address;
            }
            else {
                // DMEM
                m_Stream.Position = 0x04000000 + (address & 0x3FF);
            }


            List<RdpCommand> commands = new List<RdpCommand>();
            int count = 0;

            var swappedStream = new Swap64Stream(m_Stream);
            var swappedReader = new BinaryReader(swappedStream);

            while (count < size) {
                /* Read the command type */
                int commandId = m_Stream.ReadByte();
                m_Stream.Position--;

                // Log.Debug("RDP Command ID: " + commandId.ToString("X"));

                RdpCommandType type = DisplayListDecoder.Decode(commandId);

                /* Convert from Big-Endian to Little-Endia */
                byte[] data = new byte[type.Size];

                unsafe {
                    fixed (byte* ptr = &data[0]) {
                        for (int i = 0; i < (data.Length / 8); i++) {
                            var read = swappedReader.ReadUInt64();
                            ulong *newPtr = (ulong *)ptr + i;
                            *newPtr = read;
                            m_Stream.Position += 8;
                        }
                    }
                }

                var command = new RdpCommand(type, data);

                commands.Add(command);

                count += data.Length;
            }

            return commands;
        }
    }
}