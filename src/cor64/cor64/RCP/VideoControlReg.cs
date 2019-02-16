using System;
using System.Runtime.InteropServices;

namespace cor64.RCP
{
    public class VideoControlReg : SpecialRegisterStruct32
    {
        public const int PIXELMODE_NONE = 0;
        public const int PIXELMODE_16BPP = 2;
        public const int PIXRLMODE_32BPP = 3;

        public VideoControlReg(IntPtr ptr) : base(ptr)
        {
        }

        public int GetPixelMode()
        {
            return (int)ReadField(1, 2);
        }
    }
}
