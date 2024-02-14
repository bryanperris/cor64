using System;

namespace cor64.Mips.R4300I.TLB
{
    public class EntryLoStore
    {
        private ulong m_Value;

        private readonly static BitFiddler64 m_BitFiddler = new BitFiddler64();

        private const int F_VALID = 0;
        private const int F_DIRTY = 1;
        private const int F_CO_BITS = 2;
        private const int F_PFN = 3;

        public EntryLoStore(uint pfn, bool dirty, bool valid, PageCoherencyMode mode) {
            /* Valid */  m_BitFiddler.DefineField(1, 1);
            /* Dirty */  m_BitFiddler.DefineField(2, 1);
            /* CoBits */ m_BitFiddler.DefineField(3, 3);
            /* PFN */    m_BitFiddler.DefineField(6, 24);

            m_BitFiddler.J(F_PFN, ref m_Value, pfn & (0x3FFFFFFF >> 5));
            m_BitFiddler.J(F_DIRTY, ref m_Value, dirty);
            m_BitFiddler.J(F_VALID, ref m_Value, valid);
            m_BitFiddler.J(F_CO_BITS, ref m_Value, (byte)mode);
        }

        /// <summary>
        /// The upper bits of the physical address
        /// </summary>
        public uint PageFrameNumber => (uint)m_BitFiddler.X(F_PFN, m_Value);

        /// <summary>
        /// Gets the coherency mode that determines whether the page access hits the cache.
        /// </summary>
        public PageCoherencyMode CoherencyMode => (PageCoherencyMode)m_BitFiddler.X(F_CO_BITS, m_Value);

        /// <summary>
        /// Gets whether if the page can be written to.
        /// </summary>
        public bool IsDirty => m_BitFiddler.XB(F_DIRTY, m_Value);

        /// <summary>
        /// Gets whether the page is valid to be used in address translation.
        /// </summary>
        public bool IsValid => m_BitFiddler.XB(F_VALID, m_Value);

        public override string ToString()
        {
            return String.Format("PFN: {0:X8} D:{1} V:{2} C:{3}", PageFrameNumber, IsDirty, IsValid, CoherencyMode.ToString());
        }
    }
}