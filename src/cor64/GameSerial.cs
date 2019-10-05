using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;

namespace cor64
{
    public unsafe sealed class GameSerial
    {
        private uint m_ManufacturerID;
        private ushort m_CartID;
        private ushort m_CountryID;

        public GameSerial(byte[] data)
        {
            unsafe
            {
                fixed (byte * ptr = data)
                {
                    IntPtr p = new IntPtr(ptr);
                    m_ManufacturerID = p.AsType_32();
                    m_CartID = p.Offset(4).AsType_16();
                    m_CountryID = p.Offset(6).AsType_16();
                }
            }
        }

        public uint ManufacturerID => m_ManufacturerID;

        public ushort CartridgeID => m_CartID;

        public ushort CountryID => m_CountryID;

        public RegionType GetRegionType()
        {
            /* This logic kind of sucks, since there are so many countries and forgot Japan in here */
            switch (CountryID >> 8)
            {
                case 0x44:
                case 0x46:
                case 0x49:
                case 0x50:
                case 0x53:
                case 0x55:
                case 0x58:
                case 0x59:
                    return RegionType.PAL;

                case 0x37:
                case 0x41:
                case 0x45:
                case 0x4A:
                default:
                    return RegionType.NTSC;
            }
        }
    }
}
