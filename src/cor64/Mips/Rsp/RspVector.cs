using System.Globalization;
using System.Runtime.InteropServices;
using System;
using cor64.IO;

namespace cor64.Mips.Rsp
{
    public unsafe class RspVector : IEquatable<RspVector>
    {
        private readonly UnmanagedBuffer m_MemSource;
        private Elements* m_ElementsPtr;

        public RspVector(UnmanagedBuffer pinnedBuffer, int index)
        {
            int ptrOffset = index * 16;
            m_MemSource = new UnmanagedBuffer(pinnedBuffer, index * 16, 16);
            m_ElementsPtr = (Elements*)m_MemSource.GetPointer();
        }

        public RspVector()
        {
            m_MemSource = new UnmanagedBuffer(16);
            m_ElementsPtr = (Elements*)m_MemSource.GetPointer();
        }

        public void SetFromString(String valueBigEndian) {
            var bytes = new Byte[16];

            valueBigEndian = valueBigEndian.Replace(" ", "");
            valueBigEndian = valueBigEndian.Replace("$", "");
            valueBigEndian = valueBigEndian.Replace(",", "");

            for (int i = 0; i < 16; i++) {
                var hexByte = valueBigEndian.Substring(i * 2, 2);
                bytes[i] = Byte.Parse(hexByte, NumberStyles.HexNumber);
            }

            this.SetBytesSwapped(bytes);
        }

        public void SetBytes(byte[] vecBytes)
        {
            if (vecBytes.Length != 16)
            {
                throw new ArgumentException(nameof(vecBytes) + " must be 16 bytes exactly in length");
            }

            m_ElementsPtr->PACKED_U8[00] = vecBytes[00];
            m_ElementsPtr->PACKED_U8[01] = vecBytes[01];
            m_ElementsPtr->PACKED_U8[02] = vecBytes[02];
            m_ElementsPtr->PACKED_U8[03] = vecBytes[03];

            m_ElementsPtr->PACKED_U8[04] = vecBytes[04];
            m_ElementsPtr->PACKED_U8[05] = vecBytes[05];
            m_ElementsPtr->PACKED_U8[06] = vecBytes[06];
            m_ElementsPtr->PACKED_U8[07] = vecBytes[07];

            m_ElementsPtr->PACKED_U8[08] = vecBytes[08];
            m_ElementsPtr->PACKED_U8[09] = vecBytes[09];
            m_ElementsPtr->PACKED_U8[10] = vecBytes[10];
            m_ElementsPtr->PACKED_U8[11] = vecBytes[11];

            m_ElementsPtr->PACKED_U8[12] = vecBytes[12];
            m_ElementsPtr->PACKED_U8[13] = vecBytes[13];
            m_ElementsPtr->PACKED_U8[14] = vecBytes[14];
            m_ElementsPtr->PACKED_U8[15] = vecBytes[15];
        }

        public void SetBytesSwapped(byte[] vecBytes)
        {
            if (vecBytes.Length != 16)
            {
                throw new ArgumentException(nameof(vecBytes) + " must be 16 bytes exactly in length");
            }

            m_ElementsPtr->PACKED_U8[00] = vecBytes[15];
            m_ElementsPtr->PACKED_U8[01] = vecBytes[14];
            m_ElementsPtr->PACKED_U8[02] = vecBytes[13];
            m_ElementsPtr->PACKED_U8[03] = vecBytes[12];

            m_ElementsPtr->PACKED_U8[04] = vecBytes[11];
            m_ElementsPtr->PACKED_U8[05] = vecBytes[10];
            m_ElementsPtr->PACKED_U8[06] = vecBytes[09];
            m_ElementsPtr->PACKED_U8[07] = vecBytes[08];

            m_ElementsPtr->PACKED_U8[08] = vecBytes[07];
            m_ElementsPtr->PACKED_U8[09] = vecBytes[06];
            m_ElementsPtr->PACKED_U8[10] = vecBytes[05];
            m_ElementsPtr->PACKED_U8[11] = vecBytes[04];

            m_ElementsPtr->PACKED_U8[12] = vecBytes[03];
            m_ElementsPtr->PACKED_U8[13] = vecBytes[02];
            m_ElementsPtr->PACKED_U8[14] = vecBytes[01];
            m_ElementsPtr->PACKED_U8[15] = vecBytes[00];
        }

        public byte[] GetRawBytes()
        {
            byte[] buffer = new byte[16];
            Marshal.Copy((IntPtr)m_ElementsPtr->PACKED_U8, buffer, 0, buffer.Length);
            return buffer;
        }

        public byte[] GetRawBytesSwapped()
        {
            byte[] buffer = new byte[16];

            buffer[00] = m_ElementsPtr->PACKED_U8[15];
            buffer[01] = m_ElementsPtr->PACKED_U8[14];
            buffer[02] = m_ElementsPtr->PACKED_U8[13];
            buffer[03] = m_ElementsPtr->PACKED_U8[12];

            buffer[04] = m_ElementsPtr->PACKED_U8[11];
            buffer[05] = m_ElementsPtr->PACKED_U8[10];
            buffer[06] = m_ElementsPtr->PACKED_U8[09];
            buffer[07] = m_ElementsPtr->PACKED_U8[08];

            buffer[08] = m_ElementsPtr->PACKED_U8[07];
            buffer[09] = m_ElementsPtr->PACKED_U8[06];
            buffer[10] = m_ElementsPtr->PACKED_U8[05];
            buffer[11] = m_ElementsPtr->PACKED_U8[04];

            buffer[12] = m_ElementsPtr->PACKED_U8[03];
            buffer[13] = m_ElementsPtr->PACKED_U8[02];
            buffer[14] = m_ElementsPtr->PACKED_U8[01];
            buffer[15] = m_ElementsPtr->PACKED_U8[00];

            return buffer;
        }

        public void PackedU8(int element, byte value) {
            m_ElementsPtr->PACKED_U8[15 - element] = value;
        }

        public byte PackedU8(int element) {
            return m_ElementsPtr->PACKED_U8[15 - element];
        }

        public ushort PackedU16(int element)
        {
            return m_ElementsPtr->PACKED_U16[7 - element];
        }

        public short PackedS16(int element) {
            return m_ElementsPtr->PACKED_S16[7 - element];
        }

        public void PackedS16(int element, short value) {
            m_ElementsPtr->PACKED_S16[7 - element] = value;
        }

        public void PackedU16(int element, ushort value)
        {
            m_ElementsPtr->PACKED_U16[7 - element] = value;
        }

        public uint PackedU32(int element)
        {
            return m_ElementsPtr->PACKED_U32[3 - element];
        }

        public void PackedU32(int element, uint value)
        {
            m_ElementsPtr->PACKED_U32[3 - element] = value;
        }

        public double PackedDouble(int element)
        {
            return m_ElementsPtr->PACKED_DOUBLE[1 - element];
        }

        public void PackedDouble(int element, double value)
        {
            m_ElementsPtr->PACKED_DOUBLE[1 - element] = value;
        }

        public ulong PackedU64(int element)
        {
            return m_ElementsPtr->PACKED_U64[1 - element];
        }

        public void PackedU64(int element, ulong value)
        {
            m_ElementsPtr->PACKED_U64[1 - element] = value;
        }

        public static RspVector ScalarQuarter(RspVector vector, int e) {
            var vec = new RspVector();

            for (int i = 0; i < 8; i++) {
                vec.PackedU16(i, vector.PackedU16((i & 0xE) + (e & 1)));
            }

            return vec;
        }

         public static RspVector ScalarHalf(RspVector vector, int e) {
            var vec = new RspVector();

            for (int i = 0; i < 8; i++) {
                vec.PackedU16(i, vector.PackedU16((i & 0xC) + (e & 3)));
            }

            return vec;
        }

         public static RspVector ScalarWhole(RspVector vector, int e) {
            var vec = new RspVector();

            for (int i = 0; i < 8; i++) {
                vec.PackedU16(i, vector.PackedU16(e % 8));
            }

            return vec;
        }

        public bool Equals(RspVector other)
        {
            return m_ElementsPtr->PACKED_U64[1] == other.m_ElementsPtr->PACKED_U64[1] && m_ElementsPtr->PACKED_U64[0] == other.m_ElementsPtr->PACKED_U64[0];
        }

        public override bool Equals(object? obj) {
            return ((RspVector)obj).Equals(this);
        }

        public override int GetHashCode() {
            return this.ToString().GetHashCode();
        }

        public override string ToString() {
            return m_ElementsPtr->PACKED_U64[1].ToString("X16") + m_ElementsPtr->PACKED_U64[0].ToString("X16");
        }

        public RspVector Duplicate() {
            var vec = new RspVector();
            var bytes = this.GetRawBytes();
            vec.SetBytes(bytes);
            return vec;
        }

        public void CopyTo(ushort[] arrayVector) {
            arrayVector[0] = PackedU16(0);
            arrayVector[1] = PackedU16(1);
            arrayVector[2] = PackedU16(2);
            arrayVector[3] = PackedU16(3);
            arrayVector[4] = PackedU16(4);
            arrayVector[5] = PackedU16(5);
            arrayVector[6] = PackedU16(6);
            arrayVector[7] = PackedU16(7);
        }

        public static implicit operator RspVector(String valueBigEndian) {
            var newVec = new RspVector();
            newVec.SetFromString(valueBigEndian);
            return newVec;
        }

        public UInt128 QWord
        {
            get
            {
                return new UInt128
                {
                    hi = m_ElementsPtr->PACKED_U64[1],
                    lo = m_ElementsPtr->PACKED_U64[0]
                };
            }

            set
            {
                m_ElementsPtr->PACKED_U64[1] = value.hi;
                m_ElementsPtr->PACKED_U64[0] = value.lo;
            }
        }

        [StructLayoutAttribute(LayoutKind.Explicit, Pack = 128)]
        struct Elements
        {
            [FieldOffset(0)]
            public fixed byte PACKED_U8[16];

            [FieldOffset(0)]
            public fixed ushort PACKED_U16[8];

            [FieldOffset(0)]
            public fixed short PACKED_S16[8];

            [FieldOffset(0)]
            public fixed uint PACKED_U32[4];

            [FieldOffset(0)]
            public fixed double PACKED_DOUBLE[2];

            [FieldOffset(0)]
            public fixed ulong PACKED_U64[2];
        }
    }
}