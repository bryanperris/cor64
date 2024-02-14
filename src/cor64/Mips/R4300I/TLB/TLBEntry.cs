

using System;
using System.Collections.Generic;

namespace cor64.Mips.R4300I.TLB
{
    /// <summary>
    /// Represents a TLB entry that is used to translate a virtual address.
    /// </summary>
    public sealed class TLBEntry
    {
        private readonly EntryHiStore m_EntryHi;
        private readonly uint m_Mask; // This simply stores the mask values used in TLB read operations
        private readonly EntryLoStore m_EntryOdd; // Odd physical page entry
        private readonly EntryLoStore m_EntryEven; // Even physical page entry

        public TLBEntry(uint mask, EntryHiStore entryHi, EntryLoStore entryLo0, EntryLoStore entryLo1) {
            m_Mask = mask;
            m_EntryHi = entryHi;
            m_EntryEven = entryLo0;
            m_EntryOdd = entryLo1;
        }

        public TLBEntry() {
            m_Mask = 0;
            m_EntryHi = new EntryHiStore(0, false, 0, 0);
            m_EntryEven = new EntryLoStore(0, false, false, PageCoherencyMode.Reserved_0);
            m_EntryOdd = new EntryLoStore(0, false, false, PageCoherencyMode.Reserved_0);
        }

        public uint Mask => m_Mask;

        public EntryHiStore EntryHi => m_EntryHi;

        public EntryLoStore Even => m_EntryEven;

        public EntryLoStore Odd => m_EntryOdd;

        public bool IsGlobal => m_EntryHi.IsGlobal;

        public uint ASID => m_EntryHi.ASID;

        public bool IsValid => m_EntryEven.IsValid || m_EntryOdd.IsValid;

        public override string ToString()
        {
            return String.Format(
                "ENTRY: M:{0:X8} ASID:{1:X4} G:{2} VPN2:{3:X8} E:{4} O:{5}",
                m_Mask,
                m_EntryHi.ASID, m_EntryHi.IsGlobal, m_EntryHi.VPN2,
                m_EntryEven.ToString(),
                m_EntryOdd.ToString()
            );
        }
    }
}