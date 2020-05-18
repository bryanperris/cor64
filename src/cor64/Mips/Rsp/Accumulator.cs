using System;
using System.Runtime.InteropServices;

namespace cor64.Mips.Rsp {
    public class Accumulator {
        private readonly ulong[] m_Accumulators = new ulong[8];

        public Accumulator() {

        }

        public Accumulator(RspVector hi, RspVector mi, RspVector lo)
        {
            for (int i = 0; i < 8; i++)
            {
                Hi(i, hi.PackedU16(i));
                Mi(i, mi.PackedU16(i));
                Lo(i, lo.PackedU16(i));
            }
        }

        public ushort Hi(int index) => (ushort)(m_Accumulators[index] >> 32);

        public ushort Mi(int index) => (ushort)(m_Accumulators[index] >> 16);

        public ushort Lo(int index) => (ushort)m_Accumulators[index];

        public void Hi(int index, ushort value) => m_Accumulators[index] = ((ulong)value << 32) | (m_Accumulators[index] & 0x0000FFFFFFFFUL);
        public void Mi(int index, ushort value) => m_Accumulators[index] = ((ulong)value << 16) | (m_Accumulators[index] & 0xFFFF0000FFFFUL);
        public void Lo(int index, ushort value) => m_Accumulators[index] = ((ulong)value      ) | (m_Accumulators[index] & 0xFFFFFFFF0000UL);

        public short SignedHi(int index) => (short)Hi(index);

        public void SignedHi(int index, short value) => Hi(index, (ushort)value);

        public short SignedMi(int index) => (short)Mi(index);

        public void SignedMi(int index, short value) => Mi(index, (ushort)value);

        public short SignedLo(int index) => (short)Lo(index);

        public void SignedLo(int index, short value) => Lo(index, (ushort)value);

        public ulong this[int index] {
            get => m_Accumulators[index];
            set => m_Accumulators[index] = value;
        }

        public void ProductSignedClampMi(RspVector vector) {
            for (int i = 0; i < 8; i++) {
                vector.StoreProductClampedSignedAM(this, i);
            }
        }

        public void ProductSignedClampLo(RspVector vector) {
            for (int i = 0; i < 8; i++) {
                vector.StoreProductClampedSignedAL(this, i);
            }
        }

        public void ProductUnsignedClamp(RspVector vector) {
            for (int i = 0; i < 8; i++) {
                vector.StoreProductClampedUnsigned(this, i);
            }
        }

        public void SetLoFromVector(RspVector vector) {
            Lo(0, vector.PackedU16(0));
            Lo(1, vector.PackedU16(1));
            Lo(2, vector.PackedU16(2));
            Lo(3, vector.PackedU16(3));
            Lo(4, vector.PackedU16(4));
            Lo(5, vector.PackedU16(5));
            Lo(6, vector.PackedU16(6));
            Lo(7, vector.PackedU16(7));
        }
    }
}