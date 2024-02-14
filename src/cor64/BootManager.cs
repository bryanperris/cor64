using cor64.IO;
using cor64.Mips.R4300I;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
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
        public event Action<long> PCWrite;
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

        private void PC(long address)
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

        private void CopyIPL3(Cartridge cartridge)
        {
            /* Copies the cartridge beginning chunk (head + IPL3) into RSP data memory */

            // NOTE: instead of starting at 0x40, just start at 0 and copy everything
            // until the end of the bootloader section

            byte[] romData = new byte[Cartridge.HeaderSize + Cartridge.BootSize];

            /* read from cart into buffer */
            cartridge.RomStream.Position = 0;
            cartridge.RomStream.Read(romData, 0, romData.Length);
            cartridge.RomStream.Position = 0;

            const uint memStart = 0x04000000;
            uint memEnd = memStart + (uint)romData.Length;

            unsafe
            {
                fixed (byte* ptr = romData)
                {
                    uint* intPtr = (uint*)ptr;

                    for (uint addr = memStart; addr < memEnd; addr += 4)
                    {
                        uint romWord = *intPtr;

                        romWord = N64Endianess.U32(romWord);

                        MWR(addr, romWord);
                        intPtr++;
                    }
                }
            }

            Log.Debug("IPL3 copied into DMEM");

            // Note: The IPL3 boostrap from the cartridge clears out all RSP memory after bootup is successful
        }

        public void BootCartridge(Cartridge cartridge, bool bypassPifRom)
        {
            /**
            * 3 stages exist for the IPL which is a combo of the PIF ROM and cartridge bootloader
            * IPL 1: RCP MMU loads IPL ROM, CPU executes these instructions, copies IPL2 (from cartridge) into RSP IMEM
            * > the MMU is passing instructions to the CPU as its being executed via the SysAD bus, a slow process
            * IPL 2: CPU executes instructions, copies IPL3 (from cartridge) into RSP DMEM, computes IPL3 CRC hash, jump/execute IPL3
            * IPL 3: Last part of boot up sequence, copy 1MB of game code into RDRAM, authenticate it,IPL2/3 get cleared, jumps to game entry point
            */

            RegionType region = cartridge.Region;

            Log.Info("Cartridge Region: {0}", region.ToString());
            Log.Info("Cartridge CIC: {0}", cartridge.IPL.ToString(), region.ToString());

            // TODO: Some values are hardcoded HW values such as processor versions

            /* CoProcessor 0 Initial States HW/SW */
            WR0(CTS.CP0_REG_COUNT, 0x00005000);
            WR0(CTS.CP0_REG_SR, 0x34000000);
            WR0(CTS.CP0_REG_CAUSE, 0xB000007C);
            WR0(CTS.CP0_REG_CONFIG, 0x0006E463);
            WR0(CTS.CP0_REG_ERROR_EPC, 0xFFFFFFFF);
            WR0(CTS.CP0_REG_EPC, 0xFFFFFFFF);
            // WR0(CTS.CP0_REG_CONTEXT, 0x0000005C);
            WR0(CTS.CP0_REG_CONTEXT, 0x007FFFF0);

            MMIOWR(MMIORegWriteKind.MiVersion, 0x02020102);
            MMIOWR(MMIORegWriteKind.SpStatus, 1);

            // TODO: FPU revision reg   = 0x00000511;

            if (!bypassPifRom)
            {
                /*
                * Use the hardcoded PIF ROM address
                * CPU executes IPL1 and talks to CIC input/output lines
                * IPL1 writes os infomation into PIF RAM at offset 0x24
                */
                Log.Info("Booting via PIF ROM");
                PC(0xBFC00000);
                return;
            }
            /* PIF ROM skipped, inject the IPL1 boot values */

            Log.Info("Booting via HLE");

            // Copy IPL3 into DMEM
            CopyIPL3(cartridge);

            /* Shared values between IPL1 variants (PAL, NTSC) */
            WR(06, 0xFFFFFFFFA4001F0C);
            WR(07, 0xFFFFFFFFA4001F08);
            WR(08, 0x00000000000000C0);
            WR(10, 0x0000000000000040);
            WR(11, 0xFFFFFFFFA4000040);

            // osRomType: 0: Cart, 1: DiskDrive
            WR(19, 0);

            // osTvType
            // PAL = 0
            // NTSC = 1
            WR(20, (uint)region);

            // osResetType: 0: ColdReset, 1: NMI
            WR(21, 0);

            // osCicId
            WR(22, cartridge.IPL.Seed);

            // osVersion: 00 = 1.0, 15 = 2.5, etc
            WR(23, 0); /* S7: Unknown */

            WR(29, 0xFFFFFFFFA4001FF0); // Stack pointer

            switch (region)
            {
                case RegionType.MPAL: // Brazil PAL-M
                case RegionType.PAL:
                    {
                        WR(23, 0x0000000000000006);
                        WR(31, 0xFFFFFFFFA4001554);
                        break;
                    }
                default: {
                    WR(24, 0x0000000000000003);
                    WR(31, 0xFFFFFFFFA4001550);
                    break;
                }
            }

            /* Notes:
             * GPR5 is used with the IPL CRC checksum algorithm, its muliplied with the hardcoded seed
             */

            switch (cartridge.IPL.Cic)
            {
                default: break;
                case LockoutChipType.X101: X101Setup(region); break;
                case LockoutChipType.X102: X102Setup(region); break;
                case LockoutChipType.X103: X103Setup(region); break;
                case LockoutChipType.X105: X105Setup(region); break;
                case LockoutChipType.X106: X106Setup(region); break;
            }

            // The 6105 de-obfuscates RSP assembly based on IPL2 assembly code
            if (cartridge.IPL.Cic == LockoutChipType.X105) {
                var assembly = AssembleIPL2();
                uint address = 0x04001000;
                BinaryReader reader = new BinaryReader(assembly);

                for (int i = 0; i < assembly.Length; i+=4) {
                    var read = reader.ReadUInt32();
                    // Log.Debug("IPL2 HEX: {0:X8}", read);
                    MWR(address, read);
                    address += 4;
                }
            }


            // Execute IPL3 located in RSP DMEM + $40
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
            // WR(01, 0x0000000000000000);
            // WR(02, 0xFFFFFFFFF58B0FBF);
            // WR(03, 0xFFFFFFFFF58B0FBF);
            // WR(04, 0x0000000000000FBF);
            // WR(12, 0xFFFFFFFF9651F81E);
            // WR(13, 0x000000002D42AAC5);
            // WR(15, 0x0000000056584D60);
            // WR(22, 0x0000000000000091);
            // WR(25, 0xFFFFFFFFCDCE565F);

            WR(11, 0xFFFFFFFFA4000040); /* Temp 3 */
            WR(29, 0xFFFFFFFFA4001FF0); /* Stack Pointer */
            WR(31, 0xFFFFFFFFA4001550); /* Return Address */

            switch (region)
            {
                case RegionType.NTSC:
                    {
                        WR(05, 0x000000005493FB9A);
                        WR(14, 0xFFFFFFFFC2C20384);
                        break;
                    }
                case RegionType.MPAL:
                case RegionType.PAL:
                    {
                        WR(05, 0xFFFFFFFFDECAAAD1);
                        WR(14, 0x000000000CF85C13);
                        WR(24, 0x0000000000000002);
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

        private static Stream AssembleIPL2() {
            // This assembles a small snippet of IPL2 required by the CIC-6105

            N64Assembler assembler = new N64Assembler();
            assembler.SetQuiet();

            // TODO: Want to disable logging related to the assembler here

            /* Assembly Source */
            var source = new AssemblyTextSource("main");
            source += "arch n64.cpu";
            source += "endian msb";
            source += "lui 13,$BFC0";      // Load address 0xBFC00000 (PIF ROM START)
            source += "lw 8,$07FC(13)";    // Load word from PIF RAM at offset 0x3C
            source += "addiu 13,13,$07C0"; // increment address to 0xBFC007C0 (PIF RAM start)
            source += "andi 8,8,$0080";    // Mask out the flag
            source += "db $55,$00,$FF,$FC";// bnel 8,0,$FFFC ; Jump if the flag is true
            source += "lui 13,$BFC0";      // Load 0xBFC00000 into 13 if branch is taken
            source += "lw 8,$0024(13)";    // Load word from 0x1FC00024 (PIF ROM $24)
            source += "lui 11,$B000";      // load address 0xB0000000 (PI_BSB_DOM1_LAT_REG)

            /* Assemble into bytes */
            assembler.AddAssemblySource(source);

            Log.Info("Assembling some IPL2...");
            assembler.AssembleCode(true);
            Log.Info("Done!");

            /* Get the output */
            var streamOut = assembler.Output;
            streamOut.Position = 0;

            #if HOST_LITTLE_ENDIAN
            streamOut = new Swap32Stream(streamOut);
            #endif

            return streamOut;
        }
    }
}
