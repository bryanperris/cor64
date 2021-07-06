using System;
using System.IO;
using cor64.IO;

namespace cor64.Rdp.LLE {
    public class Rdram {
        private readonly uint m_IdxLim8;
        private readonly uint m_IdxLim16;
        private readonly uint m_IdxLim32;
        private readonly Stream m_MemoryStream;
        private readonly Stream m_BaseStream;
        private readonly BinaryReader m_Reader;
        private readonly BinaryWriter m_Writer;
        private readonly RdramHidden m_Hidden;

        private const uint RDRAM_MASK = 0x00FFFFFF;

        public Rdram(N64MemoryController.N64MemoryStream rdramStream) {
            m_Hidden = rdramStream;

            m_IdxLim8 = (uint)(rdramStream.Length - 1);
            m_IdxLim16 = (m_IdxLim8 >> 1) & 0xFFFFFFFFU;
            m_IdxLim32 = (m_IdxLim8 >> 2) & 0xFFFFFFFFU;

            m_BaseStream = rdramStream;

            #if LITTLE_ENDIAN
            m_MemoryStream = rdramStream;
            #else
            m_MemoryStream = new SwapAutoStream(rdramStream);
            #endif

            m_Reader = new BinaryReader(m_MemoryStream);
            m_Writer = new BinaryWriter(m_MemoryStream);
        }

        public void Init() {
            for (int i = 0; i < m_Hidden.HiddenLength; i++) {
                m_Hidden.HiddenWrite(i, 3);
            }
        }

        private byte ReadRdram8(uint address) {
            m_MemoryStream.Position = address;
            return (byte)m_BaseStream.ReadByte();
        }

        private void WriteRdram8(uint address, byte value) {
            m_MemoryStream.Position = address;
            m_BaseStream.WriteByte(value);
        }

        private ushort ReadRdram16(uint address) {
            m_MemoryStream.Position = address;
            return m_Reader.ReadUInt16();
        }

        public void WriteRdram16(uint address, ushort value) {
            m_MemoryStream.Position = address;
            m_Writer.Write(value);
        }

        public uint ReadRdram32(uint address) {
            m_MemoryStream.Position = address;
            return m_Reader.ReadUInt32();
        }

        public void WriteRdram32(uint address, uint value) {
            m_MemoryStream.Position = address;
            m_Writer.Write(value);
        }

        public bool ValidIdx8(uint address) {
            return address <= m_IdxLim8;
        }

        public bool ValidIdx16(uint address) {
            return address <= m_IdxLim16;
        }

        public bool ValidIdx32(uint address) {
            return address <= m_IdxLim32;
        }

        public byte ReadIdx8(uint address) {
            address &= RDRAM_MASK;

            if (ValidIdx8(address)) {
                return ReadRdram8(address);
            }
            else {
                return 0;
            }
        }

        public byte ReadIdx8Fast(uint address) {
            #if LITTLE_ENDIAN
            return ReadRdram8(address);
            #else
            return ReadRdram8(address ^ 3);
            #endif
        }

        public void WriteIdx8(uint address, byte value) {
            address &= RDRAM_MASK;
            if (ValidIdx8(address)) {
                WriteRdram8(address, value);
            }
        }

        public ushort ReadIdx16(uint address) {
            address &= RDRAM_MASK >> 1;

            if (ValidIdx16(address)) {
                return ReadRdram16(address << 1);
            }
            else {
                return 0;
            }
        }

        public ushort ReadIdx16Fast(uint address) {
            return ReadRdram16(address << 1);
        }

        public void WriteIdx16(uint address, ushort value) {
            address &= RDRAM_MASK >> 1;

            if (ValidIdx16(address)) {
                WriteRdram16(address << 1, value);
            }
        }

        public uint ReadIdx32(uint address) {
            address &= RDRAM_MASK >> 2;

            if (ValidIdx32(address)) {
                return ReadRdram32(address << 2);
            }
            else {
                return 0;
            }
        }

        public uint ReadIdx32Fast(uint address) {
            return ReadRdram32(address << 2);
        }

        public void WriteIdx32(uint address, uint value) {
            address &= RDRAM_MASK >> 2;

            if (ValidIdx32(address)) {
                WriteRdram32(address << 2, value);
            }
        }

        public void ReadPair16(ref ushort value, ref byte hiddenBit, uint address) {
            address &= RDRAM_MASK >> 1;

            if (ValidIdx16(address)) {
                value = ReadRdram16(address << 1);
                hiddenBit = m_Hidden.HiddenRead((int)address);
            }
            else {
                value = 0;
                hiddenBit = 0;
            }
        }

        public void WritePair8(uint address, byte value, byte hiddenBit) {
            address &= RDRAM_MASK;

            if (ValidIdx8(address)) {
                WriteRdram8(address, value);

                if ((address & 1) != 0) {
                    m_Hidden.HiddenWrite((int)(address >> 1), hiddenBit);
                }
            }
        }

        public void WritePair16(uint address, ushort value, byte hiddenBit) {
            address &= RDRAM_MASK >> 1;

            if (ValidIdx16(address)) {
                WriteRdram16(address << 1, value);
                m_Hidden.HiddenWrite((int)address, hiddenBit);
            }
        }

        public void WritePair32(uint address, uint value, byte hiddenBit0, byte hiddenBit1) {
            address &= RDRAM_MASK >> 2;

            if (ValidIdx32(address)) {
                WriteRdram32(address << 2, value);

                m_Hidden.HiddenWrite((int)address, hiddenBit0);
                m_Hidden.HiddenWrite((int)address + 1, hiddenBit1);
            }
        }
    }
}