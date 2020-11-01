using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands
{
    public class TextureRectangle : RdpCommand
    {
        public TextureRectangle(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public int DtDy => ReadField(1, 0, 0xFFFF);
        public int DsDx => ReadField(1, 16, 0xFFFF);
        public int TCoord => ReadField(1, 32, 0xFFFF);
        public int SCoord => ReadField(1, 48, 0xFFFF);


        public int YH => ReadField(0, 0, 0xFFF);
        public int XH => ReadField(0, 12, 0xFFF);
        public int Tile => ReadField(0, 24, 0b111);
        public int YL => ReadField(0, 32, 0xFFF);
        public int XL => ReadField(0, 44, 0xFFF);


        protected override String Params()
        {
            return String.Format("{0},{1}, {2}, {3},{4}, {5},{6}, {7},{8}",
                XL,
                YL,
                Tile,
                XH,
                YH,
                SCoord,
                TCoord,
                DsDx,
                DtDy
            );
        }
    }
}