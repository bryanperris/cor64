using System;

namespace cor64.Rdp.Commands
{
    public class ZCoeff : RdpCommand
    {
        private readonly int m_BaseOffset;

        internal ZCoeff(int baseOffset, RdpCommandType type, byte[] data) : base(type, data)
        {
            m_BaseOffset = baseOffset;
        }

        private int ReadZ(int index, int offset)
        {
            return ReadField(index, offset * 16, 0xFFFF);
        }

        public int DzDyF => ReadZ(m_BaseOffset + 1, 0);
        public int DzDyI => ReadZ(m_BaseOffset + 1, 1);
        public int DzDeF => ReadZ(m_BaseOffset + 1, 2);
        public int DzDeI => ReadZ(m_BaseOffset + 1, 3);

        public int DzDxF =>  ReadZ(m_BaseOffset, 0);
        public int DzDxI =>  ReadZ(m_BaseOffset, 1);
        public int DepthF => ReadZ(m_BaseOffset, 2);
        public int DepthI => ReadZ(m_BaseOffset, 3);


        public override String ToString()
        {
            return String.Format("ZBuffer_Coefficients {0},{1},{2},{3}, {4},{5},{6},{7}",
                (short)(ushort)DepthI,
                (short)(ushort)DepthF,
                (short)(ushort)DzDxI,
                (short)(ushort)DzDxF,
                (short)(ushort)DzDeI,
                (short)(ushort)DzDeF,
                (short)(ushort)DzDyI,
                (short)(ushort)DzDyF
            );
        }
    }
}