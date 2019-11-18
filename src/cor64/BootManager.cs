using cor64.Mips.R4300I;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{

    /* This class acts in the place of the real IPL */
    public class BootManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public event Action<int, ulong> RegWrite;
        public event Action<uint, uint> MemWrite;
        public event Action<ulong> PCWrite;
        public event Action<int, ulong> CP0Write;
        public event Action<MMIORegWriteKind, uint> MMIOWrite;

        public enum MMIORegWriteKind
        {
            MiVersion,
            SpStatus
        }

        private void WR(int index, ulong value)
        {
            RegWrite?.Invoke(index, value);
        }

        private void MWR(uint address, uint value)
        {
            MemWrite?.Invoke(address, value);
        }

        private void PC(ulong address)
        {
            PCWrite?.Invoke(address);
        }

        private void WR0(int offset, ulong address)
        {
            CP0Write?.Invoke(offset, address);
        }

        private void MMIOWR(MMIORegWriteKind kind, uint val)
        {
            MMIOWrite?.Invoke(kind, val);
        }

        private void CopyCartBootstrap(Cartridge cartridge)
        {
            /* Copies the cartridge header (head + boot) into RSP data memory */

            // NOTE: instead of starting at 0x40, just start at 0 and copy everything
            // until the end of the bootloader section

            byte[] romData = new byte[Cartridge.HeaderSize + Cartridge.BootSize];

            /* read from cart into buffer */
            cartridge.RomStream.Position = 0;
            cartridge.RomStream.Read(romData, 0, romData.Length);
            cartridge.RomStream.Position = 0;

            const uint memStart = 0xA4000000;
            uint memEnd = memStart + (uint)romData.Length;

            unsafe
            {
                fixed (byte* ptr = romData)
                {
                    uint* intPtr = (uint*)ptr;

                    for (uint addr = memStart; addr < memEnd; addr += 4)
                    {
                        MWR(addr, *intPtr);
                        intPtr++;
                    }
                }
            }

            Log.Debug("Copied cartridge header into signal processor data memory");

            // Note: The IPL3 boostrap from the cartridge clears out all RSP memory after bootup is successful
        }

        public void BootCartridge(Cartridge cartridge, bool bypassIPL)
        {
            /* Coprocess 0 setup */
            WR0(CTS.CP0_REG_RANDOM, 0x0000001F);
            WR0(CTS.CP0_REG_COUNT, 0x00005000);
            WR0(CTS.CP0_REG_SR, 0x34000000);
            WR0(CTS.CP0_REG_CAUSE, 0x0000005C);
            WR0(CTS.CP0_REG_CONFIG, 0x0006E463);
            WR0(CTS.CP0_REG_ERROR_EPC, 0xFFFFFFFF);
            WR0(CTS.CP0_REG_BADVADDR, 0xFFFFFFFF);
            WR0(CTS.CP0_REG_EPC, 0xFFFFFFFF);
            WR0(CTS.CP0_REG_CONTEXT, 0x0000005C);

            MMIOWR(MMIORegWriteKind.MiVersion, 0x02020102);
            MMIOWR(MMIORegWriteKind.SpStatus, 1);

            // TODO: FPU revision reg   = 0x00000511;

            if (!bypassIPL)
            {
                Log.Debug("Booting from PIF ROM");
                PC(0xBFC00000);
                return;
            }

            RegionType region = cartridge.Region;

            Log.Debug("Skipping system bootstrap, using initial startup state for {1}-{0}", cartridge.IPL.ToString(), region.ToString());

            CopyCartBootstrap(cartridge);

            /* Shared values */
            WR(06, 0xFFFFFFFFA4001F0C);
            WR(07, 0xFFFFFFFFA4001F08);
            WR(08, 0x00000000000000C0);
            WR(10, 0x0000000000000040);
            WR(11, 0xFFFFFFFFA4000040);

            // osRomType: 0: Cart, 1: DiskDrive
            WR(19, 0);

            // osTvType
            WR(20, (uint)region);

            // osResetType: 0: ColdReset, 1: NMI
            WR(21, 0);

            // osCicId
            WR(22, cartridge.IPL.Seed);

            // osVersion: 00 = 1.0, 15 = 2.5, etc
            WR(23, 0); /* S7: Unknown */


            WR(24, 0x0000000000000003);
            WR(29, 0xFFFFFFFFA4001FF0); // Stack pointer
            WR(31, 0xFFFFFFFFA4001550);

            /* Notes:
             * GPR5 is used with the IPL CRC checksum algorithm, its muliplied with the hardcoded seed
             */

            switch (cartridge.IPL.Cic)
            {
                default: break;
                case SecurityChipsetType.X101: X101Setup(region); break;
                case SecurityChipsetType.X102: X102Setup(region); break;
                case SecurityChipsetType.X103: X103Setup(region); break;
                case SecurityChipsetType.X105: X105Setup(region); break;
                case SecurityChipsetType.X106: X106Setup(region); break;
            }

            PC(0xA4000040);
        }

        private void X101Setup(RegionType region)
        {
            WR(22, 0x000000000000003F);
        }

        private void X102Setup(RegionType region)
        {
            WR(01, 0x0000000000000001);
            WR(02, 0x000000000EBDA536);
            WR(03, 0x000000000EBDA536);
            WR(04, 0x000000000000A536);
            WR(12, 0xFFFFFFFFED10D0B3);
            WR(13, 0x000000001402A4CC);
            WR(15, 0x000000003103E121);
            WR(22, 0x000000000000003F);
            WR(25, 0xFFFFFFFF9DEBB54F);

            switch (region)
            {
                case RegionType.NTSC:
                    {
                        WR(05, 0xFFFFFFFFC95973D5);
                        WR(14, 0x000000002449A366);
                        break;
                    }
                case RegionType.MPAL:
                case RegionType.PAL:
                    {
                        WR(05, 0xFFFFFFFFC0F1D859);
                        WR(14, 0x000000002DE108EA);
                        WR(24, 0x0000000000000000);
                        break;
                    }
                default: break;
            }
        }

        private void X103Setup(RegionType region)
        {
            WR(01, 0x0000000000000001);
            WR(02, 0x0000000049A5EE96);
            WR(03, 0x0000000049A5EE96);
            WR(04, 0x000000000000EE96);
            WR(12, 0xFFFFFFFFCE9DFBF7);
            WR(13, 0xFFFFFFFFCE9DFBF7);
            WR(15, 0x0000000018B63D28);
            WR(22, 0x0000000000000078);
            WR(25, 0xFFFFFFFF825B21C9);

            switch (region)
            {
                case RegionType.NTSC:
                    {
                        WR(05, 0xFFFFFFFF95315A28);
                        WR(14, 0x000000005BACA1DF);
                        break;
                    }
                case RegionType.MPAL:
                case RegionType.PAL:
                    {
                        WR(05, 0xFFFFFFFFD4646273);
                        WR(14, 0x000000001AF99984);
                        WR(24, 0x0000000000000000);
                        break;
                    }
                default: break;
            }
        }

        private void X105Setup(RegionType region)
        {
            WR(01, 0x0000000000000000);
            WR(02, 0xFFFFFFFFF58B0FBF);
            WR(03, 0xFFFFFFFFF58B0FBF);
            WR(04, 0x0000000000000FBF);
            WR(12, 0xFFFFFFFF9651F81E);
            WR(13, 0x000000002D42AAC5);
            WR(15, 0x0000000056584D60);
            WR(22, 0x0000000000000091);
            WR(25, 0xFFFFFFFFCDCE565F);

            MWR(0xA4001008, 0x25AD07C0);
            MWR(0xA400100C, 0x31080080);
            MWR(0xA4001010, 0x5500FFFC);
            MWR(0xA4001014, 0x3C0DBFC0);
            MWR(0xA4001018, 0x8DA80024);
            MWR(0xA400101C, 0x3C0BB000);

            switch (region)
            {
                case RegionType.NTSC:
                    {
                        WR(05, 0x000000005493FB9A);
                        WR(14, 0xFFFFFFFFC2C20384);
                        MWR(0xA4001004, 0x8DA807FC);
                        break;
                    }
                case RegionType.MPAL:
                case RegionType.PAL:
                    {
                        WR(05, 0xFFFFFFFFDECAAAD1);
                        WR(14, 0x000000000CF85C13);
                        WR(24, 0x0000000000000002);
                        MWR(0xA4001004, 0xBDA807FC);
                        break;
                    }
                default: break;
            }
        }

        private void X106Setup(RegionType region)
        {
            WR(01, 0x0000000000000000);
            WR(02, 0xFFFFFFFFA95930A4);
            WR(03, 0xFFFFFFFFA95930A4);
            WR(04, 0x00000000000030A4);
            WR(12, 0xFFFFFFFFBCB59510);
            WR(13, 0xFFFFFFFFBCB59510);
            WR(15, 0x000000007A3C07F4);
            WR(22, 0x0000000000000085);
            WR(25, 0x00000000465E3F72);

            switch (region)
            {
                case RegionType.NTSC:
                    {
                        WR(05, 0xFFFFFFFFE067221F);
                        WR(14, 0x000000005CD2B70F);
                        break;
                    }
                case RegionType.MPAL:
                case RegionType.PAL:
                    {
                        WR(05, 0xFFFFFFFFB04DC903);
                        WR(14, 0x000000001AF99984);
                        WR(24, 0x0000000000000002);
                        break;
                    }
                default: break;
            }
        }
    }
}
