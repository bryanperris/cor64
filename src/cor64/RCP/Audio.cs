using System.Runtime.InteropServices;
using System.Threading;
using cor64.IO;
using cor64.Mips;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.RCP
{

    /*
     *         
      0x0450 0000 to 0x045F FFFF  Audio interface (AI) registers:
 -----------------------------------------------------------
        AI_BASE_REG - 0x04500000

        0x0450 0000 to 0x0450 0003  AI_DRAM_ADDR_REG
                 AI DRAM address
            (W): [23:0] starting RDRAM address (8B-aligned)
        0x0450 0004 to 0x0450 0007  AI_LEN_REG
                 AI length
           (RW): [14:0] transfer length (v1.0) - Bottom 3 bits are ignored
                 [17:0] transfer length (v2.0) - Bottom 3 bits are ignored
        0x0450 0008 to 0x0450 000B  AI_CONTROL_REG
                 AI control
            (W): [0] DMA enable - if LSB == 1, DMA is enabled
        0x0450 000C to 0x0450 000F  AI_STATUS_REG
                 AI status
            (R): [31]/[0] ai_full (addr & len buffer full)
                 [30] ai_busy
                 Note that a 1to0 transition in ai_full will set interrupt
            (W): clear audio interrupt 
        0x0450 0010 to 0x0450 0013  AI_DACRATE_REG
                 AI DAC sample period register
            (W): [13:0] dac rate
                     - vid_clock/(dperiod + 1) is the DAC sample rate
                     - (dperiod + 1) >= 66 * (aclockhp + 1) must be true
        0x0450 0014 to 0x0450 0017  AI_BITRATE_REG
                 AI bit rate
            (W): [3:0] bit rate (abus clock half period register - aclockhp)
                     - vid_clock/(2*(aclockhp + 1)) is the DAC clock rate
                     - The abus clock stops if aclockhp is zero
        0x0450 0018 to 0x045F FFFF  Unused
        */

    public class Audio : PerpherialDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MemMappedBuffer m_Dram = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Length = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Control = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Status = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);
        private readonly MemMappedBuffer m_DigitalToAudioRate= new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_BitRate = new MemMappedBuffer(4);
        private readonly MipsInterface m_Interface;

        public Audio(N64MemoryController controller, MipsInterface mipsInterface) : base(controller, 0x100000)
        {
            Map(
                m_Dram,
                m_Length,
                m_Control,
                m_Status,
                m_DigitalToAudioRate,
                m_BitRate
            );

            m_Dram.Write += () => {
                // Log.Debug("Audio RDRAM Set {0:X8}", m_Dram.RegisterValue);
                m_Dram.RegisterValue &= 0x0FFFFFFF;
            };

            m_Interface = mipsInterface;

            m_Length.Write += LengthWrite;
            m_Status.Write += StatusWrite;
        }

        public void LengthWrite() {
            uint len = m_Length.RegisterValue & 0x3FFF8;

            // TODO: pass audio samples to audio backend

            m_Dram.RegisterValue += len;

            m_Interface.SetInterrupt(MipsInterface.INT_AI, true);
            m_Status.ReadonlyRegisterValue &= ~0xC0000001;

            // Log.Debug("Audio Write {0:X8}", len);
        }

        public void StatusWrite() {
            m_Interface.ClearInterrupt(MipsInterface.INT_AI);
            m_Status.RegisterValue = 0;
        }
    }
}
