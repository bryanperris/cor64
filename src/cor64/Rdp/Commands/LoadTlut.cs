using System;

namespace cor64.Rdp.Commands {
    public class LoadTlut : LoadTile {
        public LoadTlut(RdpCommandType type, byte[] data) : base(type, data)
        {
        }
    }
}