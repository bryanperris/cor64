using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cor64.Mips.Rsp
{
    public static class Cxd4StaticRsp
    {
        private static RspMemorySection RspDataMemory;

        public static void SetDMem(RspMemorySection memory) => RspDataMemory = memory;

        public static short ComputeOffset(uint inst) {
            return (short)((inst & 64) > 0 ? -(short)((~inst % 64) + 1) : (short)(inst % 64));
        }

        public static void LUV(RspVector vt, uint element, int offset, uint baseAddress)
        {
            uint addr;
            uint e = element;

            static short DMEM(uint address)
            {
                short value = RspDataMemory.U8(address);
                value <<= 7;
                return value;
            }

            addr = (uint)((baseAddress + 8 * offset) & 0x00000FFF);
            if (e != 0x0)
            {
                addr += (~e + 0x1) & 0xF;
                for (int i = 0; i < 8; i++)
                {
                    addr &= 0x00000FFF;
                    vt.PackedS16(i, DMEM(addr));
                    addr -= 16 * (uint)((e - i - 1 == 0x0) ? 1 : 0);
                    ++addr;
                }
                return;
            } /* "Mia Hamm Soccer 64" SP exception override (zilmar) */

            uint b = addr & 07;
            addr &= ~(uint)07;
            switch (b)
            {
                case 00:
                    {
                        vt.PackedS16(07, DMEM(addr + 0x007));
                        vt.PackedS16(06, DMEM(addr + 0x006));
                        vt.PackedS16(05, DMEM(addr + 0x005));
                        vt.PackedS16(04, DMEM(addr + 0x004));
                        vt.PackedS16(03, DMEM(addr + 0x003));
                        vt.PackedS16(02, DMEM(addr + 0x002));
                        vt.PackedS16(01, DMEM(addr + 0x001));
                        vt.PackedS16(00, DMEM(addr + 0x000));
                        break;
                    }
                case 01:
                    { /* PKMN Puzzle League HVQM decoder */
                        vt.PackedS16(0x000, DMEM(addr + 0x001));
                        vt.PackedS16(0x001, DMEM(addr + 0x002));
                        vt.PackedS16(0x002, DMEM(addr + 0x003));
                        vt.PackedS16(0x003, DMEM(addr + 0x004));
                        vt.PackedS16(0x004, DMEM(addr + 0x005));
                        vt.PackedS16(0x005, DMEM(addr + 0x006));
                        vt.PackedS16(0x006, DMEM(addr + 0x007));
                        addr += 0x008;
                        addr &= 0x00000FFF;
                        vt.PackedS16(0x000, DMEM(addr));
                        break;
                    }
                case 02:
                    { /* PKMN Puzzle League HVQM decoder */
                        vt.PackedS16(0x000, DMEM(addr + 0x002));
                        vt.PackedS16(0x001, DMEM(addr + 0x003));
                        vt.PackedS16(0x002, DMEM(addr + 0x004));
                        vt.PackedS16(0x003, DMEM(addr + 0x005));
                        vt.PackedS16(0x004, DMEM(addr + 0x006));
                        vt.PackedS16(0x005, DMEM(addr + 0x007));
                        addr += 0x008;
                        addr &= 0x00000FFF;
                        vt.PackedS16(0x006, DMEM(addr + 0x000));
                        vt.PackedS16(0x007, DMEM(addr + 0x001));
                        break;
                    }
                case 03:
                    { /* PKMN Puzzle League HVQM decoder */
                        vt.PackedS16(00, DMEM(addr + 0x003));
                        vt.PackedS16(01, DMEM(addr + 0x004));
                        vt.PackedS16(02, DMEM(addr + 0x005));
                        vt.PackedS16(03, DMEM(addr + 0x006));
                        vt.PackedS16(04, DMEM(addr + 0x007));
                        addr += 0x008;
                        addr &= 0x00000FFF;
                        vt.PackedS16(05, DMEM(addr + 0x000));
                        vt.PackedS16(06, DMEM(addr + 0x001));
                        vt.PackedS16(07, DMEM(addr + 0x002));
                        break;
                    }

                case 04:
                    { /* PKMN Puzzle League HVQM decoder */
                        vt.PackedS16(00, DMEM(addr + 0x004));
                        vt.PackedS16(01, DMEM(addr + 0x005));
                        vt.PackedS16(02, DMEM(addr + 0x006));
                        vt.PackedS16(03, DMEM(addr + 0x007));
                        addr += 0x008;
                        addr &= 0x00000FFF;
                        vt.PackedS16(04, DMEM(addr + 0x000));
                        vt.PackedS16(05, DMEM(addr + 0x001));
                        vt.PackedS16(06, DMEM(addr + 0x002));
                        vt.PackedS16(07, DMEM(addr + 0x003));
                        break;
                    }

                case 05:
                    {
                        /*PKMN Puzzle League HVQM decoder */
                        vt.PackedS16(00, DMEM(addr + 0x005));
                        vt.PackedS16(01, DMEM(addr + 0x006));
                        vt.PackedS16(02, DMEM(addr + 0x007));
                        addr += 0x008;
                        addr &= 0x00000FFF;
                        vt.PackedS16(03, DMEM(addr + 0x000));
                        vt.PackedS16(04, DMEM(addr + 0x001));
                        vt.PackedS16(05, DMEM(addr + 0x002));
                        vt.PackedS16(06, DMEM(addr + 0x003));
                        vt.PackedS16(07, DMEM(addr + 0x004));
                        break;
                    }
                case 06:
                    {/* PKMN Puzzle League HVQM decoder */
                        vt.PackedS16(00, DMEM(addr + 0x006));
                        vt.PackedS16(01, DMEM(addr + 0x007));
                        addr += 0x008;
                        addr &= 0x00000FFF;
                        vt.PackedS16(02, DMEM(addr + 0x000));
                        vt.PackedS16(03, DMEM(addr + 0x001));
                        vt.PackedS16(04, DMEM(addr + 0x002));
                        vt.PackedS16(05, DMEM(addr + 0x003));
                        vt.PackedS16(06, DMEM(addr + 0x004));
                        vt.PackedS16(07, DMEM(addr + 0x005));
                        break;
                    }
                case 07:
                    { /* PKMN Puzzle League HVQM decoder */
                        vt.PackedS16(00, DMEM(addr + 0x007));
                        addr += 0x008;
                        addr &= 0x00000FFF;
                        vt.PackedS16(01, DMEM(addr + 0x000));
                        vt.PackedS16(02, DMEM(addr + 0x001));
                        vt.PackedS16(03, DMEM(addr + 0x002));
                        vt.PackedS16(04, DMEM(addr + 0x003));
                        vt.PackedS16(05, DMEM(addr + 0x004));
                        vt.PackedS16(06, DMEM(addr + 0x005));
                        vt.PackedS16(07, DMEM(addr + 0x006));
                        break;
                    }
            }
        }
    }
}