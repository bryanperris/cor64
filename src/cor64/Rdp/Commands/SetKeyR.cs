using System;
namespace cor64.Rdp.Commands {
    public class SetKeyR : RdpCommand {
        public SetKeyR(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public int ScaleR => ReadField(0, 0, 0xFF);

        public int CenterR => ReadField(0, 8, 0xFF);

        public int WidthR => ReadField(0, 16, 0xFFF);

        protected override String Params() => String.Format(
            "{0}, {1}, {2}", WidthR, CenterR, ScaleR);
    }
}