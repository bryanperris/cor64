using System;
using System.Collections.Generic;

namespace cor64.Debugging
{
    public class MemoryDebugger
    {
        private List<NamedMemorySection> m_NamedPSections = new List<NamedMemorySection>();
        private Dictionary<uint, NamedMemorySection> m_Cache = new Dictionary<uint, NamedMemorySection>();

        public MemoryDebugger()
        {
            AddSection(0x00000000, 0x007FFFFF, "RDRAM Memory");
            AddSection(0x00800000, 0x03EFFFFF, "RDRAM Memory Unused");
            AddSection(0x03F00000, 0x03F00003, "RDRAM Reg: Config");
            AddSection(0x03F00004, 0x03F00007, "RDRAM Reg: DeviceID");
            AddSection(0x03F00008, 0x03F0000B, "RDRAM Reg: Delay");
            AddSection(0x03F0000C, 0x03F0000F, "RDRAM Reg: Mode");
            AddSection(0x03F00010, 0x03F00013, "RDRAM Reg: Ref - Interval");
            AddSection(0x03F00014, 0x03F00017, "RDRAM Reg: Ref - Row");
            AddSection(0x03F00018, 0x03F0001B, "RDRAM Reg: Ras - Interval");
            AddSection(0x03F0001C, 0x03F0001F, "RDRAM Reg: Min - Interval");
            AddSection(0x03F00020, 0x03F00023, "RDRAM Reg: Address Select");
            AddSection(0x03F00024, 0x03F00027, "RDRAM Reg: Manufacturer");
            AddSection(0x03F00028, 0x03FFFFFF, "RDRAM Reg: Unknown");
            AddSection(0x04000000, 0x04000FFF, "SP Data Memory");
            AddSection(0x04001000, 0x04001FFF, "SP Instruction Memory");
            AddSection(0x04040000, 0x04040003, "SP Reg: DMA Master Address Select");
            AddSection(0x04040004, 0x04040007, "SP Reg: DMA Slave Address Select");
            AddSection(0x04040008, 0x0404000B, "SP Reg: DMA Read Length");
            AddSection(0x0404000C, 0x0404000F, "SP Reg: DMA Write Length");
            AddSection(0x04040010, 0x04040013, "SP Reg: Status");
            AddSection(0x04040014, 0x04040017, "SP Reg: DMA Full");
            AddSection(0x04040018, 0x0404001B, "SP Reg: DMA Busy");
            AddSection(0x0404001C, 0x0404001F, "SP Reg: Semaphore");
            AddSection(0x04040020, 0x0407FFFF, "SP Unused A");
            AddSection(0x04080000, 0x04080003, "SP Reg: Program Counter");
            AddSection(0x04080004, 0x04080007, "SP Reg: Built-in Software Testing");
            AddSection(0x04080008, 0x040FFFFF, "SP Unused B");
            AddSection(0x04100000, 0x04100003, "DP Reg: Display List DMA Start Address");
            AddSection(0x04100004, 0x04100007, "DP Reg: Display List DMA End Address");
            AddSection(0x04100008, 0x0410000B, "DP Reg: Display List DMA Current Address");
            AddSection(0x0410000C, 0x0410000F, "DP Reg: Status");
            AddSection(0x04100010, 0x04100013, "DP Reg: Clock Counter");
            AddSection(0x04100014, 0x04100017, "DP Reg: Buffer Busy Counter");
            AddSection(0x04100018, 0x0410001B, "DP Reg: Pipe Busy Counter");
            AddSection(0x0410001C, 0x0410001F, "DP Reg: Texture Memory Load Counter");
            AddSection(0x04100020, 0x041FFFFF, "DP Unused A");
            AddSection(0x04200000, 0x04200003, "DP TMEM Built-in Software Test");
            AddSection(0x04200004, 0x04200007, "DP Span Test Mode");
            AddSection(0x04200008, 0x0420000B, "DP Span Test Address");
            AddSection(0x0420000C, 0x0420000F, "DP Span Test Data");
            AddSection(0x04200010, 0x042FFFFF, "DP Unused B");
            AddSection(0x04300000, 0x04300003, "RCP Interface Reg: Init Mode");
            AddSection(0x04300004, 0x04300007, "RCP Interface Reg: Version");
            AddSection(0x04300008, 0x0430000B, "RCP Interface Reg: Interrupt");
            AddSection(0x0430000C, 0x0430000F, "RCP Interface Reg: Interrupt Mask");
            AddSection(0x04300010, 0x043FFFFF, "RCP Interface Unused");
            AddSection(0x04400000, 0x04400003, "Video Interface Reg: Status/Control");
            AddSection(0x04400004, 0x04400007, "Video Interface Reg: Framebuffer Address");
            AddSection(0x04400008, 0x0440000B, "Video Interface Reg: Horizonal Line Width");
            AddSection(0x0440000C, 0x0440000F, "Video Interface Reg: Vertical Interrupt");
            AddSection(0x04400010, 0x04400013, "Video Interface Reg: Current Vertical Line");
            AddSection(0x04400014, 0x04400017, "Video Interface Reg: Video Timing (burst control)");
            AddSection(0x04400018, 0x0440001B, "Video Interface Reg: Vertical Sync");
            AddSection(0x0440001C, 0x0440001F, "Video Interface Reg: Horizontal Sync");
            AddSection(0x04400020, 0x04400023, "Video Interface Reg: Horizontal Sync Leap");
            AddSection(0x04400024, 0x04400027, "Video Interface Reg: Horizontal Start");
            AddSection(0x04400028, 0x0440002B, "Video Interface Reg: Vertical Start");
            AddSection(0x0440002C, 0x0440002F, "Video Interface Reg: Vertical Burst");
            AddSection(0x04400030, 0x04400033, "Video Interface Reg: X Scale");
            AddSection(0x04400034, 0x04400037, "Video Interface Reg: Y Scale");
            AddSection(0x04400038, 0x044FFFFF, "Video Interface Unused");
            AddSection(0x04500000, 0x04500003, "Audio Interface Reg: DMA Address");
            AddSection(0x04500004, 0x04500007, "Audio Interface Reg: DMA Transfer Length");
            AddSection(0x04500008, 0x0450000B, "Audio Interface Reg: DMA Control");
            AddSection(0x0450000C, 0x0450000F, "Audio Interface Reg: Status");
            AddSection(0x04500010, 0x04500013, "Audio Interface Reg: DAC Sample Rate");
            AddSection(0x04500014, 0x04500017, "Audio Interface Reg: Bitrate");
            AddSection(0x04500018, 0x045FFFFF, "Audio Interface Unused");
            AddSection(0x04600000, 0x04600003, "PI Reg: DMA Address");
            AddSection(0x04600004, 0x04600007, "PI Reg: DMA Cart Address");
            AddSection(0x04600008, 0x0460000B, "PI Reg: DMA Read Length");
            AddSection(0x0460000C, 0x0460000F, "PI Reg: DMA Write Length");
            AddSection(0x04600010, 0x04600013, "PI Reg: Status");
            AddSection(0x04600014, 0x04600017, "PI Reg: Dom1 Latency");
            AddSection(0x04600018, 0x0460001B, "PI Reg: Dom1 Pulse Width");
            AddSection(0x0460001C, 0x0460001F, "PI Reg: Dom1 Page Size");
            AddSection(0x04600020, 0x04600023, "PI Reg: Dom1 Release Duration");
            AddSection(0x04600024, 0x04600027, "PI Reg: Dom2 Latency");
            AddSection(0x04600028, 0x0460002B, "PI Reg: Dom2 Pulse Width");
            AddSection(0x0460002C, 0x0460002F, "PI Reg: Dom2 Page Size");
            AddSection(0x04600030, 0x04600033, "PI Reg: Dom2 Release Duration");
            AddSection(0x04600034, 0x046FFFFF, "PI Unused");
            AddSection(0x04700000, 0x04700003, "RDRAM Interface Reg: Mode");
            AddSection(0x04700004, 0x04700007, "RDRAM Interface Reg: Config");
            AddSection(0x04700008, 0x0470000B, "RDRAM Interface Reg: Load");
            AddSection(0x0470000C, 0x0470000F, "RDRAM Interface Reg: Select");
            AddSection(0x04700010, 0x04700013, "RDRAM Interface Reg: Refresh");
            AddSection(0x04700014, 0x04700017, "RDRAM Interface Reg: Latency");
            AddSection(0x04700018, 0x0470001B, "RDRAM Interface Reg: Read Error");
            AddSection(0x0470001C, 0x0470001F, "RDRAM Interface Reg: Write Error");
            AddSection(0x04700020, 0x047FFFFF, "RDRAM Interface Unused");
            AddSection(0x04800000, 0x04800003, "Serial Interface: DMA Address");
            AddSection(0x04800004, 0x04800007, "Serial Interface: DMA PIF Address (64-Byte read)");
            AddSection(0x04800008, 0x0480000B, "Serial Interface: Reserved");
            AddSection(0x0480000C, 0x0480000F, "Serial Interface: Reserved");
            AddSection(0x04800010, 0x04800013, "Serial Interface: DMA PIF Address (64-Byte write)");
            AddSection(0x04800014, 0x04800017, "Serial Interface: Reserved");
            AddSection(0x04800018, 0x0480001B, "Serial Interface: Status");
            AddSection(0x0480001C, 0x048FFFFF, "Serial Interface Unused");
            AddSection(0x04900000, 0x04FFFFFF, "Unused");
            AddSection(0x05000000, 0x05FFFFFF, "Dom2 Address 1");
            AddSection(0x06000000, 0x07FFFFFF, "Dom1 Address 1");
            AddSection(0x08000000, 0x0FFFFFFF, "Dom2 Address 2");
            AddSection(0x10000000, 0x1FBFFFFF, "Dom1 Address 2 (Cartridge)");
            AddSection(0x1FC00000, 0x1FC007BF, "PIF Bootrom");
            AddSection(0x1FC007C0, 0x1FC007FF, "PIF RAM");
            AddSection(0x1FC00800, 0x1FCFFFFF, "Reserved");
            AddSection(0x1FD00000, 0x7FFFFFFF, "Dom1 Address 3");
            AddSection(0x80000000, 0xFFFFFFFF, "External SysAD");
        }

        private void AddSection(uint b, uint e, string n)
        {
            m_NamedPSections.Add(new NamedMemorySection(b, e, n));
        }

        public String GetMemName(uint address)
        {
            switch (address)
            {
                default: break;
                case 0x300: return "osTvType";
                case 0x304: return "osRomType";
                case 0x308: return "osRomBase";
                case 0x30C: return "osResetType";
                case 0x310: return "osCicId";
                case 0x314: return "osVersion";
                case 0x318: return "osMemSize";
                case 0x31C: return "osAppNMIBuffer";
            }

            if (m_Cache.Count >= 30000)
            {
                m_Cache.Clear();
            }

            if (m_Cache.ContainsKey(address))
            {
                return m_Cache[address].Name;
            }

            foreach (var section in m_NamedPSections)
            {
                if (address >= section.Start && address <= section.End)
                {
                    m_Cache.Add(address, section);
                    return section.Name;
                }
            }

            return "Unknown";
        }
    }
}
