using System;
namespace cor64.Rdp.Commands {
    public class SetPrimDepth : RdpCommand {
        public SetPrimDepth(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public int DeltaZ => ReadField(0, 0, 0xFFFF);

        public int Z => ReadField(0, 16, 0xFFFF);

        protected override String Params() => String.Format(
            "{0}, {1}",
            Z,
            DeltaZ);
    }
}