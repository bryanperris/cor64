using System.Runtime.CompilerServices;
using System.Text;
using System;
namespace cor64.Rdp.Commands
{
    public class TextureRectangleFlip : TextureRectangle
    {
        public TextureRectangleFlip(RdpCommandType type, byte[] data) : base(type, data)
        {
        }
    }
}