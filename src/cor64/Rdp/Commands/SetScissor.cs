using System;
namespace cor64.Rdp.Commands {
    public class SetScissor : RdpCommand {
        public SetScissor(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        private const int MASK = 0xFFF;

        public int BottomRightY => ReadField(0, MASK);
        public int BottomRightX => ReadField(12, MASK);
        public bool KeepOddLines => ReadFlag(24);
        public bool FieldEnable => ReadFlag(25);
        public int TopLeftY => ReadField(32, MASK);
        public int TopLeftX => ReadField(44, MASK);

        protected override String Params() => 
            String.Format("{0},{1}, {2},{3}, {4},{5}",
                TopLeftX,
                TopLeftY,
                FieldEnable ? "SCISSOR_FIELD" : "0",
                KeepOddLines ? "SCISSOR_ODD" : "0",
                BottomRightX,
                BottomRightY);
    }
}