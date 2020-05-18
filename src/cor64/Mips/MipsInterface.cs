﻿using cor64.IO;
using NLog;

/* The Mips Interface (MI)
 * ---------------------------
 * This is the CPU chip interface used by the N64 hardware
 * - Interrupts: The main board triggers these interrupts from external events, only one is used as a timer and its managed by the processor itself
 * 
 *         MI_BASE_REG - 0x04300000

        0x0430 0000 to 0x0430 0003  MI_INIT_MODE_REG or MI_MODE_REG
                 MI init mode
            (W): [6:0] init length        (R): [6:0] init length
                 [7] clear init mode           [7] init mode
                 [8] set init mode             [8] ebus test mode
                 [9/10] clr/set ebus test mode [9] RDRAM reg mode
                 [11] clear DP interrupt
                 [12] clear RDRAM reg
                 [13] set RDRAM reg mode
        0x0430 0004 to 0x0430 0007  MI_VERSION_REG or MI_NOOP_REG
                 MI version
            (R): [7:0] io
                 [15:8] rac
                 [23:16] rdp
                 [31:24] rsp
        0x0430 0008 to 0x0430 000B  MI_INTR_REG
                 MI interrupt
            (R): [0] SP intr
                 [1] SI intr
                 [2] AI intr
                 [3] VI intr
                 [4] PI intr
                 [5] DP intr
        0x0430 000C to 0x0430 000F  MI_INTR_MASK_REG
                 MI interrupt mask
            (W): [0/1] clear/set SP mask  (R): [0] SP intr mask
                 [2/3] clear/set SI mask       [1] SI intr mask
                 [4/5] clear/set AI mask       [2] AI intr mask
                 [6/7] clear/set VI mask       [3] VI intr mask
                 [8/9] clear/set PI mask       [4] PI intr mask
                 [10/11] clear/set DP mask     [5] DP intr mask
        0x0430 0010 to 0x043F FFFF  Unused

 */

namespace cor64.Mips
{
    public class MipsInterface : PerpherialDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private MemMappedBuffer m_Mode = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);
        private MemMappedBuffer m_Version = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private MemMappedBuffer m_Interrupt = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private MemMappedBuffer m_Mask = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);
        private BitFiddler m_IntFiddler = new BitFiddler();
        private BitFiddler m_MaskFiddler = new BitFiddler();

        public const int INT_SP = 0;
        public const int INT_SI = 1;
        public const int INT_AI = 2;
        public const int INT_VI = 3;
        public const int INT_PI = 4;
        public const int INT_DP = 5;

        public const int MASK_CLEAR = 1;
        public const int MASK_SET = 2;

        public MipsInterface(N64MemoryController controller) : base(controller, 0x100000)
        {
            Map(m_Mode, m_Version, m_Interrupt, m_Mask);

            for (int i = 0; i < INT_DP + 1; i++)
            {
                m_IntFiddler.DefineField(i, 1);
                m_MaskFiddler.DefineField(i * 2, 2);
            }

            m_Mask.Write += () => {
                ProcessMaskClearSet();
            };
        }

        public void SetVersion(uint value)
        {
            m_Version.ReadonlyRegisterValue = value;
        }

        private bool ReadIntBool(int index)
        {
            uint val = m_Interrupt.ReadonlyRegisterValue;
            m_IntFiddler.X(index, ref val);
            return !(val == 0);
        }

        private bool ReadMaskBool(int index)
        {
            uint val = m_Mask.ReadonlyRegisterValue;
            m_IntFiddler.X(index, ref val);
            return !(val == 0);
        }

        public void ProcessMaskClearSet()
        {
            uint value = m_Mask.RegisterValue;

#if DEBUG_FULL
            Log.Debug("MI Interrupt Mask was modified: " + value.ToString("X8"));
#endif

            uint clearset_sp = m_MaskFiddler.X(INT_SP, ref value);
            uint clearset_si = m_MaskFiddler.X(INT_SI, ref value);
            uint clearset_ai = m_MaskFiddler.X(INT_AI, ref value);
            uint clearset_vi = m_MaskFiddler.X(INT_VI, ref value);
            uint clearset_pi = m_MaskFiddler.X(INT_PI, ref value);
            uint clearset_dp = m_MaskFiddler.X(INT_DP, ref value);

            if (clearset_sp != 0)
            {
                SetMask(INT_SP, clearset_sp == MASK_SET);
            }

            if (clearset_si != 0)
            {
                SetMask(INT_SI, clearset_si == MASK_SET);
            }

            if (clearset_ai != 0)
            {
                SetMask(INT_AI, clearset_ai == MASK_SET);
            }

            if (clearset_vi != 0)
            {
                SetMask(INT_VI, clearset_vi == MASK_SET);
            }

            if (clearset_pi != 0)
            {
                SetMask(INT_PI, clearset_pi == MASK_SET);
            }

            if (clearset_dp != 0)
            {
                SetMask(INT_DP, clearset_dp == MASK_SET);
            }
        }

        public uint Interrupt => m_Interrupt.ReadonlyRegisterValue;

        public uint Mask => m_Interrupt.ReadonlyRegisterValue;

        public bool IntSP => ReadIntBool(INT_SP);

        public bool IntSI => ReadIntBool(INT_SI);

        public bool IntAI => ReadIntBool(INT_AI);

        public bool IntVI => ReadIntBool(INT_VI);

        public bool IntPI => ReadIntBool(INT_PI);

        public bool IntDP => ReadIntBool(INT_DP);

        public bool IntMaskSP => ReadMaskBool(INT_SP);

        public bool IntMaskSI => ReadMaskBool(INT_SI);

        public bool IntMaskAI => ReadMaskBool(INT_AI);

        public bool IntMaskVI => ReadMaskBool(INT_VI);

        public bool IntMaskPI => ReadMaskBool(INT_PI);

        public bool IntMaskDP => ReadMaskBool(INT_DP);

        public void SetInterrupt(int index, bool value)
        {
            uint val = value ? 1U : 0;
            uint reg = m_Interrupt.ReadonlyRegisterValue;
            m_IntFiddler.J(index, ref reg, val);
            m_Interrupt.ReadonlyRegisterValue = reg;
        }

        private void SetMask(int index, bool value)
        {
#if DEBUG_FULL
            string v = value ? "Enabled" : "Disabled";

            switch (index)
            {
                default: break;
                case INT_SP: Log.Debug("SP Interrupt " + v); break;
                case INT_SI: Log.Debug("SI Interrupt " + v); break;
                case INT_AI: Log.Debug("AI Interrupt " + v); break;
                case INT_VI: Log.Debug("VI Interrupt " + v); break;
                case INT_PI: Log.Debug("PI Interrupt " + v); break;
                case INT_DP: Log.Debug("DP Interrupt " + v); break;
            }
#endif

            uint val = value ? 1U : 0;
            uint reg = m_Mask.ReadonlyRegisterValue;
            m_IntFiddler.J(index, ref reg, val);
            m_Mask.ReadonlyRegisterValue = reg;
        }
    }
}