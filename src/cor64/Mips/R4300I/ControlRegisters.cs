using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.Mips.R4300I.TLB;
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

        public TLBRegisters TLBRegs { get; } = new TLBRegisters();

        private const uint CAUSE_MASK = 0x00000300;
        private const uint STATUS_MASK = 0xFE20FFFF;

        private uint m_LocalCount;

        public event Action<int> TLBRegisterWrite;

        public ControlRegisters()
        {
            m_Registers = new ulong[SIZE];
            m_WriteMap = new Action<int, ulong>[SIZE];
            m_ReadMap = new Func<int, ulong>[SIZE];

            m_Registers[CTS.CP0_REG_CONFIG] = 0x7006E463;

            for (int i = 0; i < SIZE; i++)
            {
                if (TLBRegs.IsTLBRegister(i)) {
                    m_ReadMap[i] = TLBRegs.RegisterRead;
                    m_WriteMap[i] = TLBRegs.RegisterWrite;
                    continue;
                }

                if (i == CTS.CP0_REG_CAUSE)
                {
                    m_ReadMap[i] = (_) => Cause.Value;
                    m_WriteMap[i] = (_, x) => Cause.Value = (uint)x;
                }
                else if (i == CTS.CP0_REG_SR)
                {
                    m_ReadMap[i] = (_) => Status.Read();
                    m_WriteMap[i] = (_, x) => Status.Write((uint)x);
                }
                else if (i == CTS.CP0_REG_COUNT) {
                    m_ReadMap[i] = (o) => m_Registers[o];
                    m_WriteMap[i] = (o, x) => {
                        m_LocalCount = (uint)x;
                        m_Registers[o] = x;
                    };
                }
                else if (i == CTS.CP0_REG_PRID) {
                    m_WriteMap[i] = VoidWrite;
                    m_ReadMap[i] = (_) => 0x00000B22;
                }
                else if (i == 7 || (i >= 21 && i <= 25)) {
                    m_ReadMap[i] = VoidRead;
                    m_WriteMap[i] = VoidWrite;
                }
                else if (i == 31) {
                    // Unknown behavior here based on this test: https://github.com/PeterLemon/N64/tree/master/CPUTest/CP0/COP0Register
                    m_WriteMap[i] = (_, x) => {
                        m_Registers[31] = x;
                        m_Registers[21] = x;
                    };

                    m_ReadMap[i] = (_) => m_Registers[31];
                }
                else if (i == CTS.CP0_REG_CONFIG) {
                    m_ReadMap[i] = (_) => m_Registers[CTS.CP0_REG_CONFIG];
                    m_WriteMap[i] = (_,x) => {
                        m_Registers[CTS.CP0_REG_CONFIG] &= ~0xFUL;
                        m_Registers[CTS.CP0_REG_CONFIG] |= x & 0xF;
                    };
                }
                else
                {
                    m_ReadMap[i] = (o) => m_Registers[o];
                    m_WriteMap[i] = (o, x) => m_Registers[o] = x;
                }
            }
        }

        private void TLBRegWrite(int i, ulong value) {
            m_Registers[i] = value;
            TLBRegisterWrite?.Invoke(i);
        }

        public ulong Read(int i)
        {
            return m_ReadMap[i](i);
        }

        public void Write(int i, ulong value)
        {   
            m_WriteMap[i](i, value);
        }

        public uint Count => m_LocalCount;

        private void VoidWrite(int i, ulong value) {
            // Do nothing
        }

        private ulong VoidRead(int i) {
            return 0;
        }

        public ulong CpuRegRead(int i)
        {
// #if DEBUG_COPROCESSOR
//             Log.Debug("Cop0 Reg Read: {0}", ABI.GetLabel("", ABI.RegType.Cop0, i));
// #endif

            return m_ReadMap[i](i);
        }

        public void CpuRegWrite(int i, ulong value)
        {
            #if DEBUG_COPROCESSOR
                        Log.Debug("Cop0 Reg Write: {0} {1:X16}", ABI.GetLabel("", ABI.RegType.Cop0, i), value);
            #endif

            // Properly handle writes to CAUSE
            if (i == CTS.CP0_REG_CAUSE) {
                #if DEBUG_CAUSE_REG
                Log.Debug("Cop0 Cause Write: {0:X8}", value);
                #endif

                value = (uint)((m_ReadMap[i](i) & ~CAUSE_MASK) | (value & CAUSE_MASK));
            }

            // Properly handle writes to STATUS
            if (i == CTS.CP0_REG_SR) {
                value = (uint)((m_ReadMap[i](i) & ~STATUS_MASK) | (value & STATUS_MASK));
            }

            m_WriteMap[i](i, value);
        }
    }
}
