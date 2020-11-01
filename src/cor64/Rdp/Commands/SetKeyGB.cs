using System;
namespace cor64.Rdp.Commands
{
    public class SetKeyGB : RdpCommand
    {
        public SetKeyGB(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        public int ScaleB => ReadField(0, 0, 0xFF);

        public int CenterB => ReadField(0, 8, 0xFF);

        public int ScaleG => ReadField(0, 16, 0xFF);

        public int CenterG => ReadField(0, 24, 0xFF);

        public int WidthB => ReadField(0, 32, 0xFFF);

        public int WidthG => ReadField(0, 44, 0xFFF);

        protected override String Params() => String.Format(
            "{0}, {1}, {2}, {3}, {4}, {5}", WidthG, WidthB, CenterG, ScaleG, CenterB, ScaleB);
    }
}