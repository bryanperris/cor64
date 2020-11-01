using System;
using System.Runtime.InteropServices;
using cor64.IO;

namespace cor64.RCP
{
    public class VideoControlReg
    {
        private MemMappedBuffer m_Buffer;
        public const int PIXELMODE_NONE = 0;
        public const int PIXELMODE_16BPP = 2;
        public const int PIXELMODE_32BPP = 3;
        private BitFiddler m_Fiddler = new BitFiddler();

        const int F_PIXELMODE = 0;

        public VideoControlReg(MemMappedBuffer buffer)
        {
            m_Buffer = buffer;
            m_Fiddler.DefineField(0, 2);
        }

        public int GetPixelMode()
        {
            uint v = m_Buffer.RegisterValue;
            return (int)m_Fiddler.X(F_PIXELMODE, ref v);
        }
    }
}
