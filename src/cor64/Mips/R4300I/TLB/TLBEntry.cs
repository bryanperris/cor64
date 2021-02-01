

using System;
using System.Collections.Generic;

namespace cor64.Mips.R4300I.TLB
{
    /// <summary>
    /// Represents a TLB entry that is used to translate a virtual address.
    /// </summary>
    public sealed class TLBEntry
    {
        private readonly EntryHiRegister m_EntryHi;
        private readonly PageMask m_Mask; // This simply stores the mask values used in TLB read operations
        private readonly EntryLoRegister m_EntryOdd; // Odd physical page entry
        private readonly EntryLoRegister m_EntryEven; // Even physical page entry

        public TLBEntry(EntryHiRegister entryHi, PageMask mask, EntryLoRegister entryLo0, EntryLoRegister entryLo1) {
            m_Mask = mask;
            m_EntryHi = entryHi.MakeCacheCopy();
            m_EntryEven = entryLo0.MakeCacheCopy();
            m_EntryOdd = entryLo1.MakeCacheCopy();
        }

        public TLBEntry() {
            m_Mask = new PageMask(0);
            m_EntryHi =new EntryHiRegister();
            m_EntryEven = new EntryLoRegister();
            m_EntryOdd = new EntryLoRegister();
        }

        public PageMask Mask => m_Mask;

        public EntryHiRegister VPN2 => m_EntryHi;

        public EntryLoRegister Even => m_EntryEven;

        public EntryLoRegister Odd => m_EntryOdd;

        public override string ToString()
        {
            return String.Format(
                "ENTRY: M:{0:X8} H:{1:X16} E:{2:X16} O:{3:X16}",
                m_Mask.Value,
                m_EntryHi.Read(),
                m_EntryEven.Read(),
                m_EntryOdd.Read()
            );
        }
    }
}