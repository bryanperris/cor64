using System;
namespace cor64.Rdp.Commands {
    public class SetConvert : RdpCommand {
        public SetConvert(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        private int ReadTerm (int index) {
            return ReadField(0, index * 9, 0x1FF);
        }

        private int Sign(int v) {
            // 9-bit signed conversion

            if (v >= 0x100)
                return -(0x1FF - v + 1);
            else
                return v & 0xFF;
        }

        public int K5 => Sign(ReadTerm(0));

        public int K4 => Sign(ReadTerm(1));

        public int K3 => Sign(ReadTerm(2));

        public int K2 => Sign(ReadTerm(3));

        public int K1 => Sign(ReadTerm(4));

        public int K0 => Sign(ReadTerm(5));

        protected override String Params() => String.Format(
            "{0}, {1}, {2}, {3}, {4}, {5}", K0, K1, K2, K3, K4, K5);
    }
}