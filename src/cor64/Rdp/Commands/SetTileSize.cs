using System;

namespace cor64.Rdp.Commands {
    public class SetTileSize : LoadTile {
        public SetTileSize(RdpCommandType type, byte[] data) : base(type, data)
        {
        }
    }
}