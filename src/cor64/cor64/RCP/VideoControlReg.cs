using System;
using System.Runtime.InteropServices;
using cor64.IO;

namespace cor64.RCP
{
    public class VideoControlReg : SpecialRegister32
    {
        private IntPtr m_Ptr;
        public const int PIXELMODE_NONE = 0;
        public const int PIXELMODE_16BPP = 2;
        public const int PIXELMODE_32BPP = 3;

        public override uint Value {
            get => m_Ptr.AsType_32Swp();
            set => m_Ptr.AsType_32Swp(value);
        }

        public VideoControlReg(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        public int GetPixelMode()
        {
            return (int)ReadField(1, 2);
        }

        protected override void InternalSet(uint value)
        {
            m_Ptr.AsType_32Swp(value);
        }
    }
}
