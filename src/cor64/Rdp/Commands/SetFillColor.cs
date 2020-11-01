using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands {
    public class SetFillColor : RdpCommand {
        public SetFillColor(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public uint PackedColor => ReadFieldU32(0, 0xFFFFFFFFU);

        protected override String Params() {
            return "$" + PackedColor.ToString("X8");
        }
    }
}