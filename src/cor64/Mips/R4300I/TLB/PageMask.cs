

using System;

namespace cor64.Mips.R4300I.TLB
{
    // public enum NamedPageSize
    // {
    //     SizeUnknown,
    //     Size4KB,
    //     Size16KB,
    //     Size64KB,
    //     Size256KB,
    //     Size1MB,
    //     Size4MB,
    //     Size16MB
    // }

    // TODO: Clean up this file

    public struct PageMask
    {
        private readonly uint m_Value;
        private readonly int m_Size;
        private readonly uint m_Mask;
        private readonly uint m_SearchKey;
        private readonly int m_NumBits;

        public PageMask(uint pageMaskReg)
        {
            m_Value = pageMaskReg;
            m_Mask = (pageMaskReg >> 1) | 0x0FFFU;
            m_Size = (int)(m_Mask + 1);
            m_SearchKey = (pageMaskReg >> 13) & 0xFFF;

            m_NumBits = m_Size switch
            {
                0x0001000 => 12,
                0x0004000 => 14,
                0x0010000 => 16,
                0x0040000 => 18,
                0x0100000 => 20,
                0x0400000 => 22,
                0x1000000 => 24,
                _ => 0,
            };
        }

        // public PageSize(int pageSize)
        // {
        //     m_Size = pageSize;
        // }

        // public PageSize(NamedPageSize namedSize)
        // {
        //     if (namedSize > NamedPageSize.SizeUnknown && namedSize <= NamedPageSize.Size16MB)
        //     {
        //         switch (namedSize)
        //         {
        //             case NamedPageSize.Size4KB: m_Size = 0x1000; break;
        //             case NamedPageSize.Size16KB: m_Size = 0x4000; break;
        //             case NamedPageSize.Size64KB: m_Size = 0x1000 << 4; break;
        //             case NamedPageSize.Size256KB: m_Size = 0x4000 << 4; break;
        //             case NamedPageSize.Size1MB: m_Size = 0x1000 << 8; break;
        //             case NamedPageSize.Size4MB: m_Size = 0x4000 << 8; break;
        //             case NamedPageSize.Size16MB: m_Size = 0x1000 << 12; break;
        //             default: m_Size = 0; break;
        //         }
        //     }
        //     else
        //     {
        //         m_Size = 0;
        //     }
        // }

        public int Size => m_Size;

        public uint Mask => m_Mask;

        public uint Value => m_Value;

        public uint VPNMask => ~(m_Value | 0x1FFFU);

        public int VPN2Shift => 1 + m_NumBits;

        public uint SearchKey => m_SearchKey;

        // public NamedPageSize NamedSize
        // {
        //     get
        //     {
        //         switch (m_Size)
        //         {
        //             case 0x1000: return NamedPageSize.Size4KB;
        //             case 0x4000: return NamedPageSize.Size16KB;
        //             case 0x10000: return NamedPageSize.Size64KB;
        //             case 0x40000: return NamedPageSize.Size256KB;
        //             case 0x100000: return NamedPageSize.Size1MB;
        //             case 0x400000: return NamedPageSize.Size4MB;
        //             case 0x1000000: return NamedPageSize.Size16MB;
        //             default: return NamedPageSize.SizeUnknown;
        //         }
        //     }
        // }

        // public uint AddressOffsetMask
        // {
        //     get
        //     {
        //         return (uint)(m_Size - 1);
        //     }
        // }

        // public ulong PageMask
        // {
        //     get
        //     {
        //         return (((ulong)m_Size - 1) & 0x1FFE000) << 1;
        //     }
        // }

        // public override string ToString()
        // {
        //     return NamedSize.ToString();
        // }
    }
}