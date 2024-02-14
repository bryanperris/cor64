

using System;

namespace cor64.Mips.R4300I.TLB
{
    public class EntryHiStore
    {
        private ulong m_Value;

        private readonly BitFiddler64 m_BitFiddler = new BitFiddler64();

        private const int F_ASID = 0;
        private const int F_GLOBAL = 1;
        private const int F_VPN2_64 = 2;
        private const int F_REGION = 3;

        public EntryHiStore(uint asid, bool global, uint vpn2, byte region) {
            /* ASID */    m_BitFiddler.DefineField(0, 8);
            /* G */  m_BitFiddler.DefineField(12, 1);
            /* VPN2 64 */ m_BitFiddler.DefineField(13, 27);
            /* R*/ m_BitFiddler.DefineField(62, 2);

            m_BitFiddler.J(F_ASID, ref m_Value, asid);
            m_BitFiddler.J(F_GLOBAL, ref m_Value, global);
            m_BitFiddler.J(F_VPN2_64, ref m_Value, vpn2);
            m_BitFiddler.J(F_REGION, ref m_Value, region);
        }

        public uint ASID => (uint)m_BitFiddler.X(F_ASID, m_Value);

        public bool IsGlobal => m_BitFiddler.XB(F_GLOBAL, m_Value);

        public uint VPN2 => (uint)m_BitFiddler.X(F_VPN2_64, m_Value);

        public byte Region => (byte)m_BitFiddler.X(F_REGION, m_Value);
    }
}