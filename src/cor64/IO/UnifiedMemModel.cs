using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    /*
     *   0x0000 0000 to 0x03EF FFFF RDRAM Memory
         0x03F0 0000 to 0x03FF FFFF RDRAM Registers
         0x0400 0000 to 0x040F FFFF SP Registers
         0x0410 0000 to 0x041F FFFF DP Command Registers
         0x0420 0000 to 0x042F FFFF DP Span Registers
         0x0430 0000 to 0x043F FFFF MIPS Interface (MI) Registers
         0x0440 0000 to 0x044F FFFF Video Interface (VI) Registers
         0x0450 0000 to 0x045F FFFF Audio Interface (AI) Registers
         0x0460 0000 to 0x046F FFFF Peripheral Interface (PI) Registers
         0x0470 0000 to 0x047F FFFF RDRAM Interface (RI) Registers
         0x0480 0000 to 0x048F FFFF Serial Interface (SI) Registers
         0x0490 0000 to 0x04FF FFFF Unused
         0x0500 0000 to 0x05FF FFFF Cartridge Domain 2 Address 1
         0x0600 0000 to 0x07FF FFFF Cartridge Domain 1 Address 1
         0x0800 0000 to 0x0FFF FFFF Cartridge Domain 2 Address 2
         0x1000 0000 to 0x1FBF FFFF Cartridge Domain 1 Address 2
         0x1FC0 0000 to 0x1FC0 07BF PIF ROM
         0x1FC0 07C0 to 0x1FC0 07FF PIF RAM
         0x1FC0 0800 to 0x1FCF FFFF Reserved
         0x1FD0 0000 to 0x7FFF FFFF Cartridge Domain 1 Address 3
         0x8000 0000 to 0xFFFF FFFF External SysAD Device
     */

    public class UnifiedMemModel<T>
    {
        private T[] m_MemMap = new T[0xFFFF];
        private uint[] m_BaseMap = new uint[0xFFFF];

        private void Map(T t, uint start, uint end)
        {
            uint b = start << 16;

            for (uint i = start; i <= end; i++)
            {
                m_MemMap[i] = t;
                m_BaseMap[i] = b;
            }
        }

        public virtual void Init()
        {
            Map(RDRAM,      0x0000, 0x03EF);
            Map(RDRAMRegs,  0x03F0, 0x03FF);
            Map(SPRegs,     0x0400, 0x040F);
            Map(DPCmdRegs,  0x0410, 0x041F);
            Map(DPSpanRegs, 0x0420, 0x042F);
            Map(MIRegs,     0x0430, 0x043F);
            Map(VIRegs,     0x0440, 0x044F);
            Map(AIRegs,     0x0450, 0x045F);
            Map(PIRegs,     0x0460, 0x046F);
            Map(RIRegs,     0x0470, 0x047F);
            Map(SIRegs,     0x0480, 0x048F);
            Map(Unused,     0x0490, 0x04FF);
            Map(DiskDriveRegisters,     0x0500, 0x05FF);
            Map(Cart,       0x1000, 0x1FBF);
            Map(PIF,        0x1FC0, 0x1FCF);
            Map(Unused,     0x1FD0, 0x7FFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetDevice(uint address)
        {
            try
            {
                uint x = address >> 16;
                return m_MemMap[x];
            }
            catch (Exception)
            {
                throw new IndexOutOfRangeException(
                    String.Format("Address {0:X8} is out of range of the device memory map", address));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetBaseAddress(uint address)
        {
            uint x = address >> 16;
            return m_BaseMap[x];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetDeviceOffset(uint address)
        {
            uint b = GetBaseAddress(address);
            return address - b;
        }

        public T RDRAM { get; set; }

        public T RDRAMRegs { get; set; }

        public T SPRegs { get; set; }

        public T DPCmdRegs { get; set; }

        public T DPSpanRegs { get; set; } 

        public T MIRegs { get; set; }

        public T VIRegs { get; set; }

        public T AIRegs { get; set; }

        public T PIRegs { get; set; }

        public T RIRegs { get; set; }

        public T SIRegs { get; set; }

        public T Cart { get; set; }

        public T PIF { get; set; }

        public T DiskDriveRegisters { get; set; }

        public T Unused { get; set; }
    }
}
