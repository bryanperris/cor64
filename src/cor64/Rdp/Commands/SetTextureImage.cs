using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands {
    public class SetTextureImage : SetColorImage {
        public SetTextureImage(RdpCommandType type, byte[] data) : base(type, data)
        {
        }
    }
}