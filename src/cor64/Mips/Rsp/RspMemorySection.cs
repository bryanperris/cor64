using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cor64.IO;

namespace cor64.Mips.Rsp
{
    public class RspMemorySection : IMemoryAccess
    {
        private readonly N64MemoryController m_PhysicalMemory;
        private readonly long m_Offset;

        public RspMemorySection(N64MemoryController memory, long offset) {
            m_PhysicalMemory = memory;
            m_Offset = offset;
        }

        public short S16(long address) => m_PhysicalMemory.S16(m_Offset + address);

        public void S16(long address, short value) => m_PhysicalMemory.S16(m_Offset + address, value);

        public int S32(long address)  => m_PhysicalMemory.S32(m_Offset + address);

        public void S32(long address, int value)  => m_PhysicalMemory.S32(m_Offset + address, value);

        public long S64(long address)  => m_PhysicalMemory.S64(m_Offset + address);

        public void S64(long address, long value)  => m_PhysicalMemory.S64(m_Offset + address, value);

        public sbyte S8(long address)  => m_PhysicalMemory.S8(m_Offset + address);

        public void S8(long address, sbyte value)  => m_PhysicalMemory.S8(m_Offset + address, value);

        public ushort U16(long address)  => m_PhysicalMemory.U16(m_Offset + address);

        public void U16(long address, ushort value)  => m_PhysicalMemory.U16(m_Offset + address, value);

        public uint U32(long address)  => m_PhysicalMemory.U32(m_Offset + address);

        public void U32(long address, uint value)  => m_PhysicalMemory.U32(m_Offset + address, value);

        public ulong U64(long address)  => m_PhysicalMemory.U64(m_Offset + address);

        public void U64(long address, ulong value)  => m_PhysicalMemory.U64(m_Offset + address, value);

        public byte U8(long address)  => m_PhysicalMemory.U8(m_Offset + address);

        public void U8(long address, byte value)  => m_PhysicalMemory.U8((m_Offset + address), value);
    }
}