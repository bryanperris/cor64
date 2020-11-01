using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands {
    public class FillRectangle : RdpCommand {
        public FillRectangle(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public int TopLeftY => ReadField(0, 0xFFF);

        public int TopLeftX => ReadField(12, 0xFFF);

        public int BottomRightY => ReadField(32, 0xFFF);

        public int BottomRightX => ReadField(44, 0xFFF);

        protected override String Params() {
            return String.Format(
                "{0},{1}, {2},{3}",
                BottomRightX,
                BottomRightY,
                TopLeftX,
                TopLeftY);
        }
    }
}