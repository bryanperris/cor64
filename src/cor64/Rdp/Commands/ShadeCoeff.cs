using System;

namespace cor64.Rdp.Commands
{
    public class ShadeCoeff : RdpCommand
    {
        private readonly int m_BaseOffset;

        internal ShadeCoeff(int baseOffset, RdpCommandType type, byte[] data) : base(type, data)
        {
            m_BaseOffset = baseOffset;
        }

        private int ReadColorDelta(int index, int offset)
        {
            return ReadField(index, offset * 16, 0xFFFF);
        }

        private int ReadColor(int index, int offset)
        {
            return ReadField(index, offset * 16, 0xFFFF);
        }

        public int DaDyF => ReadColorDelta(m_BaseOffset + 7, 0);
        public int DbDyF => ReadColorDelta(m_BaseOffset + 7, 1);
        public int DgDyF => ReadColorDelta(m_BaseOffset + 7, 2);
        public int DrDyF => ReadColorDelta(m_BaseOffset + 7, 3);

        public int DaDeF => ReadColorDelta(m_BaseOffset + 6, 0);
        public int DbDeF => ReadColorDelta(m_BaseOffset + 6, 1);
        public int DgDeF => ReadColorDelta(m_BaseOffset + 6, 2);
        public int DrDeF => ReadColorDelta(m_BaseOffset + 6, 3);

        public int DaDyI => ReadColorDelta(m_BaseOffset + 5, 0);
        public int DbDyI => ReadColorDelta(m_BaseOffset + 5, 1);
        public int DgDyI => ReadColorDelta(m_BaseOffset + 5, 2);
        public int DrDyI => ReadColorDelta(m_BaseOffset + 5, 3);

        public int DaDeI => ReadColorDelta(m_BaseOffset + 4, 0);
        public int DbDeI => ReadColorDelta(m_BaseOffset + 4, 1);
        public int DgDeI => ReadColorDelta(m_BaseOffset + 4, 2);
        public int DrDeI => ReadColorDelta(m_BaseOffset + 4, 3);

        public int DaDxF => ReadColorDelta(m_BaseOffset + 3, 0);
        public int DbDxF => ReadColorDelta(m_BaseOffset + 3, 1);
        public int DgDxF => ReadColorDelta(m_BaseOffset + 3, 2);
        public int DrDxF => ReadColorDelta(m_BaseOffset + 3, 3);

        public int AlphaF => ReadColor(m_BaseOffset + 2, 0);
        public int BlueF =>  ReadColor(m_BaseOffset + 2, 1);
        public int GreenF => ReadColor(m_BaseOffset + 2, 2);
        public int RedF =>   ReadColor(m_BaseOffset + 2, 3);

        public int DaDxI => ReadColorDelta(m_BaseOffset + 1, 0);
        public int DbDxI => ReadColorDelta(m_BaseOffset + 1, 1);
        public int DgDxI => ReadColorDelta(m_BaseOffset + 1, 2);
        public int DrDxI => ReadColorDelta(m_BaseOffset + 1, 3);

        public int AlphaI => ReadColor(m_BaseOffset, 0);
        public int BlueI =>  ReadColor(m_BaseOffset, 1);
        public int GreenI => ReadColor(m_BaseOffset, 2);
        public int RedI =>   ReadColor(m_BaseOffset, 3);

        public override String ToString()
        {
            return String.Format("Shade_Coefficients {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                Params_ColorI(RedI, GreenI, BlueI, AlphaI),
                Params_ColorI(DrDxI, DgDxI, DbDxI, DaDxI),
                Params_ColorF(RedF, GreenF, BlueF, AlphaF),
                Params_ColorF(DrDxF, DgDxF, DbDxF, DaDxF),
                Params_ColorI(DrDeI, DgDeI, DbDeI, DaDeI),
                Params_ColorI(DrDyI, DgDyI, DbDyI, DaDyI),
                Params_ColorF(DrDeF, DgDeF, DbDeF, DaDeF),
                Params_ColorF(DrDyF, DgDyF, DbDyF, DaDyF)
            );
        }

        private String Params_ColorI(int r, int g, int b, int a)
        {
            return String.Format("{0},{1},{2},{3}", (short)(ushort)r, (short)(ushort)g, (short)(ushort)b, (short)(ushort)a);
        }

        private String Params_ColorF(int r, int g, int b, int a)
        {
            return String.Format("{0},{1},{2},{3}", (short)(ushort)r, (short)(ushort)g, (short)(ushort)b, (short)(ushort)a);
        }
    }
}