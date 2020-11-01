using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace cor64.Mips.R4300I
{
    public class ControlRegisters
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        const int SIZE = 32;
        private readonly ulong[] m_Registers;
        private readonly Action<int, ulong>[] m_WriteMap;
        private readonly Func<int, ulong>[] m_ReadMap;

        public CauseRegister Cause { get; } = new CauseRegister();
        public StatusRegister Status { get; } = new StatusRegister();

        public ControlRegisters()
        {
            m_Registers = new ulong[SIZE];
            m_WriteMap = new Action<int, ulong>[SIZE];
            m_ReadMap = new Func<int, ulong>[SIZE];

            for (int i = 0; i < SIZE; i++)
            {
                if (i == CTS.CP0_REG_CAUSE)
                {
                    m_ReadMap[i] = (o) => Cause.Value;
                    m_WriteMap[i] = (o, x) => Cause.Value = (uint)x;
                }
                else if (i == CTS.CP0_REG_SR)
                {
                    m_ReadMap[i] = (o) => Status.Read();
                    m_WriteMap[i] = (o, x) => Status.Write((uint)x);
                }
                else
                {
                    m_ReadMap[i] = (o) => m_Registers[o];
                    m_WriteMap[i] = (o, x) => m_Registers[o] = x;
                }
            }
        }

        public ulong Read(int i)
        {
            return m_ReadMap[i](i);
        }

        public void Write(int i, ulong value)
        {
            m_WriteMap[i](i, value);
        }

        public ulong RegRead(int i)
        {
// #if DEBUG_COPROCESSOR
//             Log.Debug("Cop0 Reg Read: {0}", ABI.GetLabel("", ABI.RegType.Cop0, i));
// #endif

            return m_ReadMap[i](i);
        }

        public void RegWrite(int i, ulong value)
        {
#if DEBUG_COPROCESSOR
            Log.Debug("Cop0 Reg Write: {0} {1:X16}", ABI.GetLabel("", ABI.RegType.Cop0, i), value);
#endif

            m_WriteMap[i](i, value);

            // On timer compare write, clear the count and timer pending interrupt
            if (i == CTS.CP0_REG_COMPARE) {
                m_WriteMap[CTS.CP0_REG_COUNT](CTS.CP0_REG_COUNT, 0);
                Cause.ClearPendingInterrupt(7);

#if DEBUG_MIPS_TIMER
                Log.Debug("Mips Timer Compare set to {0}", value);
#endif
            }


#if DEBUG_INTERRUPTS
            if (i == CTS.CP0_REG_EPC) {
                Log.Debug("EPC was modified by CPU");
            }

            if (i == CTS.CP0_REG_EPC) {
                Log.Debug("Error EPC was modified by CPU");
            }
#endif
        }
    }
}
