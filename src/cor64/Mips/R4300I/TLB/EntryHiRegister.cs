

using System;

namespace cor64.Mips.R4300I.TLB
{
    public class EntryHiRegister
    {
        private ulong m_Value;

        private readonly BitFiddler64 m_BitFiddler = new BitFiddler64();

        private const int F_ASID = 0;
        private const int F_VPN2 = 1;
        private const int F_REGION = 2;

        public EntryHiRegister() {
            /* ASID */    m_BitFiddler.DefineField(0, 8);
            /* VPN2 */ m_BitFiddler.DefineField(13, 27);
            /* Region */  m_BitFiddler.DefineField(62, 2);
        }

        public void Write(ulong value) {
            value &= 0xC00000FFFFFFE0FF;
            m_Value = value;
        }

        public ulong Read() {
            return m_Value;
        }

        public uint ASID {
            get => (uint)m_BitFiddler.X(F_ASID, m_Value);
            set => m_BitFiddler.J(F_ASID, ref m_Value, value);
        }

        public uint VPN2 {
            get => (uint)m_BitFiddler.X(F_VPN2, m_Value);
            set => m_BitFiddler.J(F_VPN2, ref m_Value, value);
        }

        public byte Region {
            get => (byte)m_BitFiddler.X(F_REGION, m_Value);
            set => m_BitFiddler.J(F_REGION, ref m_Value, value);
        }

        public override string ToString()
        {
            return $"ASID: {ASID:X4} VPN2: {VPN2:X8} REGION: {Region:X2}";
        }
    }
}