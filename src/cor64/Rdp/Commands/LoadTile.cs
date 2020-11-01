using System;

namespace cor64.Rdp.Commands {
    public class LoadTile : RdpCommand {
        public LoadTile(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public int HighT => ReadField(0, 0xFFF);

        public int HighS => ReadField(12, 0xFFF);

        /// <summary>
        /// Tile Descriptor Index
        /// </summary>
        /// <returns></returns>
        public int Tile => ReadField(24, 0b11);

        public int LowT => ReadField(32, 0xFFF);

        public int LowS => ReadField(44, 0xFFF);

        protected override String Params() {
            return String.Format("{0},{1}, {2}, {3},{4}",
                LowS,
                LowT,
                Tile,
                HighS,
                HighT);
        }
    }
}