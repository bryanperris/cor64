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

        public IReadOnlyList<RdpCommand> ReadDisplayList(long address, int size) {
            List<RdpCommand> commands = new List<RdpCommand>();
            int count = 0;
            
            m_Stream.Position = address;

            var swappedStream = new Swap64Stream(m_Stream);
            var swappedReader = new BinaryReader(swappedStream);

            while (count < size) {
                /* Read the command type */
                /* Always mask out the upper 2 unused bits (in case its signed) */
                int commandId = m_Stream.ReadByte() & 0b00111111;
                m_Stream.Position--;

                // Log.Debug("RDP Command ID: " + commandId.ToString("X"));

                RdpCommandType type = DisplayListDecoder.Decode(commandId);

                /* Convert from Big-Endian to Little-Endia */
                byte[] data = new byte[type.Size];

                unsafe {
                    fixed (byte* ptr = &data[0]) {
                        for (int i = 0; i < (data.Length / 8); i++) {
                            var read = swappedReader.ReadUInt64();

                            /* The first 2 upper bits are never used, mask them out in case of sign extensions */
                            if (i == 0) {
                                read &= 0x3FFFFFFFFFFFFFFF;
                            }

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