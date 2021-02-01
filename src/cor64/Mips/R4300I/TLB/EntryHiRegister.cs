

using System;

namespace cor64.Mips.R4300I.TLB
{
    public class EntryHiRegister
    {
        private ulong m_Value;

        private readonly BitFiddler64 m_BitFiddler = new BitFiddler64();

        private const int F_ASID = 0;
        private const int F_VPN2_32 = 1;
        private const int F_VPN2_64 = 2;
        private const int F_REGION = 3;


        public EntryHiRegister() {
            /* ASID */    m_BitFiddler.DefineField(0, 8);
            /* VPN2 32 */ m_BitFiddler.DefineField(13, 19);
            /* VPN2 64 */ m_BitFiddler.DefineField(13, 27);
            /* Region */  m_BitFiddler.DefineField(62, 2);
        }

        public void Write(ulong value) {
            value &= 0xC00000FFFFFFE0FF;
            m_Value = value;
        }

        public ulong Read() {
            return m_Value;
        }

        public byte ASID {
            get => (byte)m_BitFiddler.X(F_ASID, ref m_Value);
            set => m_BitFiddler.J(F_ASID, ref m_Value, value);
        }

        public uint GetVPN2(bool isOp64, int offsetSize) {
            uint vpn2 = 0;

            if (isOp64) {
                vpn2 = (uint)m_BitFiddler.X(F_VPN2_64, ref m_Value);
            }
            else {
                vpn2 = (uint)m_BitFiddler.X(F_VPN2_32, ref m_Value);
            }

            vpn2 <<= 13; // Turn it into the mapped virtual base address
            vpn2 >>= offsetSize; // Get the VPN2 based on page mask

            return vpn2;
        }

        public void SetVPN2(uint vpn2) {
            m_BitFiddler.J(F_VPN2_64, ref m_Value, vpn2);
        }

        public EntryHiRegister MakeCacheCopy() {
            var copy = new EntryHiRegister();
            copy.Write(Read());
            return copy;
        }
    }
}