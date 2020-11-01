using System;

namespace cor64.Rdp.Commands
{
    public class TextureCoeff : RdpCommand
    {
        private readonly int m_BaseOffset;

        internal TextureCoeff(int baseOffset, RdpCommandType type, byte[] data) : base(type, data)
        {
            m_BaseOffset = baseOffset;
        }

        private int ReadCoordDelta(int index, int offset)
        {
            return ReadField(index, offset * 16, 0xFFFF);
        }

        private int ReadCoord(int index, int offset)
        {
            return ReadField(index, offset * 16, 0xFFFF);
        }

        public int DwDyF => ReadCoordDelta(m_BaseOffset + 7, 1);
        public int DtDyF => ReadCoordDelta(m_BaseOffset + 7, 2);
        public int DsDyF => ReadCoordDelta(m_BaseOffset + 7, 3);

        public int DwDeF => ReadCoordDelta(m_BaseOffset + 6, 1);
        public int DtDeF => ReadCoordDelta(m_BaseOffset + 6, 2);
        public int DsDeF => ReadCoordDelta(m_BaseOffset + 6, 3);

        public int DwDyI => ReadCoordDelta(m_BaseOffset + 5, 1);
        public int DtDyI => ReadCoordDelta(m_BaseOffset + 5, 2);
        public int DsDyI => ReadCoordDelta(m_BaseOffset + 5, 3);

        public int DwDeI => ReadCoordDelta(m_BaseOffset + 4, 1);
        public int DtDeI => ReadCoordDelta(m_BaseOffset + 4, 2);
        public int DsDeI => ReadCoordDelta(m_BaseOffset + 4, 3);

        public int DwDxF => ReadCoordDelta(m_BaseOffset + 3, 1);
        public int DtDxF => ReadCoordDelta(m_BaseOffset + 3, 2);
        public int DsDxF => ReadCoordDelta(m_BaseOffset + 3, 3);

        public int WCoordF => ReadCoord(m_BaseOffset + 2, 1);
        public int TCoordF => ReadCoord(m_BaseOffset + 2, 2);
        public int SCoordF => ReadCoord(m_BaseOffset + 2, 3);

        public int DwDxI => ReadCoordDelta(m_BaseOffset + 1, 1);
        public int DtDxI => ReadCoordDelta(m_BaseOffset + 1, 2);
        public int DsDxI => ReadCoordDelta(m_BaseOffset + 1, 3);

        public int WCoordI => ReadCoord(m_BaseOffset, 1);
        public int TCoordI => ReadCoord(m_BaseOffset, 2);
        public int SCoordI => ReadCoord(m_BaseOffset, 3);

        public override String ToString()
        {
            return String.Format("Texture_Coefficients {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                Params_CoordI(SCoordI, TCoordI, WCoordI),
                Params_CoordI(DsDxI, DtDxI, DwDxI),
                Params_CoordF(SCoordF, TCoordF, WCoordF),
                Params_CoordF(DsDxF, DtDxF, DwDxF),
                Params_CoordI(DsDeI, DtDeI, DwDeI),
                Params_CoordI(DsDyI, DtDyI, DwDyI),
                Params_CoordF(DsDeF, DtDeF, DwDeF),
                Params_CoordF(DsDyF, DtDyF, DwDyF)
            );
        }

        private String Params_CoordI(int s, int t, int w)
        {
            return String.Format("{0},{1},{2}", (short)(ushort)s, (short)(ushort)t, (short)(ushort)w);
        }

        private String Params_CoordF(int s, int t, int w)
        {
            return String.Format("{0},{1},{2}", (short)(ushort)s, (short)(ushort)t, (short)(ushort)w);
        }
    }
}