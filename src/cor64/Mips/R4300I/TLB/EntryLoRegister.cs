using System;

namespace cor64.Mips.R4300I.TLB
{
    public class EntryLoRegister
    {
        private ulong m_Value;

        private readonly static BitFiddler64 m_BitFiddler = new BitFiddler64();

        private const int F_GLOBAL = 0;
        private const int F_VALID = 1;
        private const int F_DIRTY = 2;
        private const int F_CO_BITS = 3;
        private const int F_PFN = 4;

        static EntryLoRegister() {
            /* Global */ m_BitFiddler.DefineField(0, 1);
            /* Valid */  m_BitFiddler.DefineField(1, 1);
            /* Dirty */  m_BitFiddler.DefineField(2, 1);
            /* CoBits */ m_BitFiddler.DefineField(3, 3);
            /* PFN */    m_BitFiddler.DefineField(6, 24);
        }

        public void Write(ulong value) {
            m_Value = value;
        }

        public ulong Read() {
            return m_Value;
        }

        /// <summary>
        /// The upper bits of the physical address
        /// </summary>
        public uint PageFrameNumber {
            get => (uint)m_BitFiddler.X(F_PFN, ref m_Value);
            set => m_BitFiddler.J(F_PFN, ref m_Value, value);
        }

        /// <summary>
        /// Gets the coherency mode that determines whether the page access hits the cache.
        /// </summary>
        public PageCoherencyMode CoherencyMode {
            get => (PageCoherencyMode)m_BitFiddler.X(F_CO_BITS, ref m_Value);
            set => m_BitFiddler.J(F_CO_BITS, ref m_Value, (byte)value);
        }

        /// <summary>
        /// Gets whether if the page can be written to.
        /// </summary>
        public bool IsDirty {
            get => m_BitFiddler.XB(F_DIRTY, ref m_Value);
            set => m_BitFiddler.J(F_DIRTY, ref m_Value, value);
        }

        /// <summary>
        /// Gets whether the page is valid to be used in address translation.
        /// </summary>
        public bool IsValid {
            get => m_BitFiddler.XB(F_VALID, ref m_Value);
            set => m_BitFiddler.J(F_VALID, ref m_Value, value);
        }

        /// <summary>
        /// Gets whether the page ignores the ASID feature.
        /// </summary>
        public bool IsGlobal {
            get => m_BitFiddler.XB(F_GLOBAL, ref m_Value);
            set => m_BitFiddler.J(F_GLOBAL, ref m_Value, value);
        }

        public EntryLoRegister MakeCacheCopy() {
            var copy = new EntryLoRegister();
            copy.Write(Read());
            return copy;
        }
    }
}