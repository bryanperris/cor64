using System;
using System.IO;
using System.Security.Cryptography;
using cor64.IO;

namespace cor64.Utils
{
	public sealed class CartridgeRomChecksum : HashAlgorithm
    {
        public const Int32 InputSize = 0x101000;

        private byte[] m_Hashcode = new byte[8];
        private SecurityChipsetType m_CicType;

		public CartridgeRomChecksum(SecurityChipsetType type)
        {
            m_CicType = type;
        }

        protected override void HashCore(byte[] source, int ibStart, int cbSize)
        {
            /* The source buffer must be converted to little-endian from big-endian */
            BinaryReader arrayReader = new BinaryReader(new Swap32Stream(new MemoryStream(source)));
            BinaryWriter hashWriter = new BinaryWriter(new MemoryStream(m_Hashcode));

            if (cbSize < InputSize)
                throw new ArgumentOutOfRangeException("cbSize must be 0x10100 or bigger");

            uint seed;

            switch (m_CicType)
            {
				case SecurityChipsetType.X101:
				case SecurityChipsetType.X102: seed = 0xF8CA4DDC; break;
				case SecurityChipsetType.X103: seed = 0xA3886759; break;
				case SecurityChipsetType.X105: seed = 0xDF26F436; break;
				case SecurityChipsetType.X106: seed = 0x1FEA617A; break;
                default: return;
            }

            // Do the CIC-NUS checksum algorithm (found in n64sums.c)
            // Based on uCON64's N64 checksum algorithm by Andreas Stebbins
            // Copyright (0) 2005 Parasyte

            uint t1, t2, t3, t4, t5, t6;
            t1 = t2 = t3 = t4 = t5 = t6 = seed;
            uint read = 0;
            uint r = 0;

            arrayReader.BaseStream.Position = 0x1000;
            for (int i = 0; i < 0x100000; i += 4)
            {
                read = arrayReader.ReadUInt32();

                if ((t6 + read) < t6) t4++;
                t6 += read;
                t3 ^= read;
                r = RotateLeft(read, (int)(read & 0x1F));
                t5 += r;
                if (t2 > read) t2 ^= r;
                else t2 ^= t6 ^ read;

				if (m_CicType == SecurityChipsetType.X105)
                {
                    long oldPos = arrayReader.BaseStream.Position;
                    arrayReader.BaseStream.Position = 0x40 + 0x0710 + (i & 0xFF);
                    t1 += arrayReader.ReadUInt32() ^ read;
                    arrayReader.BaseStream.Position = oldPos;
                }
                else
                {
                    t1 += t5 ^ read;
                }
            }

			if (m_CicType == SecurityChipsetType.X103)
            {
                hashWriter.Write((UInt32)((t6 ^ t4) + t3));
                hashWriter.Write((UInt32)((t5 ^ t2) + t1));
            }
			else if (m_CicType == SecurityChipsetType.X106)
            {
                hashWriter.Write((UInt32)((t6 * t4) + t3));
                hashWriter.Write((UInt32)((t5 * t2) + t1));
            }
            else
            {
                hashWriter.Write((UInt32)(t6 ^ t4 ^ t3));
                hashWriter.Write((UInt32)(t5 ^ t2 ^ t1));
            }
        }

        protected override byte[] HashFinal()
        {
            return m_Hashcode;
        }

        public override int HashSize
        {
            get
            {
                return m_Hashcode.Length;
            }
        }

        private uint RotateLeft(uint value, int bits)
        {
            return (value << bits) | (value >> (32 - bits));
        }

        public uint CRC1
        {
            get { return (uint) (m_Hashcode[0] | (m_Hashcode[1] << 8) | (m_Hashcode[2] << 16) | (m_Hashcode[3] << 24)); }
        }

        public uint CRC2
        {
            get { return (uint) (m_Hashcode[4] | (m_Hashcode[5] << 8) | (m_Hashcode[6] << 16) | (m_Hashcode[7] << 24)); }
        }

        public uint CRC1Le
        {
            get { return (uint) (m_Hashcode[3] | (m_Hashcode[2] << 8) | (m_Hashcode[1] << 16) | (m_Hashcode[0] << 24)); }
        }

        public uint CRC2Le
        {
            get { return (uint) (m_Hashcode[7] | (m_Hashcode[6] << 8) | (m_Hashcode[5] << 16) | (m_Hashcode[4] << 24)); }
        }

        public void UpdateRomChecksum(byte[] romSource) {
            romSource[0x10] = m_Hashcode[3];
            romSource[0x11] = m_Hashcode[2];
            romSource[0x12] = m_Hashcode[1];
            romSource[0x13] = m_Hashcode[0];

            romSource[0x14] = m_Hashcode[7];
            romSource[0x15] = m_Hashcode[6];
            romSource[0x16] = m_Hashcode[5];
            romSource[0x17] = m_Hashcode[4];
        }

        public void UpdateRomChecksum(Stream romSource) {
            romSource.Position = 0x10;

            romSource.WriteByte(m_Hashcode[3]);
            romSource.WriteByte(m_Hashcode[2]);
            romSource.WriteByte(m_Hashcode[1]);
            romSource.WriteByte(m_Hashcode[0]);

            romSource.WriteByte(m_Hashcode[7]);
            romSource.WriteByte(m_Hashcode[6]);
            romSource.WriteByte(m_Hashcode[5]);
            romSource.WriteByte(m_Hashcode[4]);
        }

        public override void Initialize()
        {
        }
    }
}
