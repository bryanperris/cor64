using System;
namespace cor64.Rdp.Commands {
    public class SetEnvColor : RdpCommand {
        public SetEnvColor(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        private int ReadColor (int index) {
            return ReadField(0, index * 8, 0xFF);
        }

        public int Alpha => ReadColor(0);

        public int Blue => ReadColor(1);

        public int Green => ReadColor(2);

        public int Red => ReadColor(3);

        protected override String Params() => String.Format(
            "${0}{1}{2}{3}",
            Red.ToString("X2"),
            Green.ToString("X2"),
            Blue.ToString("X2"),
            Alpha.ToString("X2"));
    }
}