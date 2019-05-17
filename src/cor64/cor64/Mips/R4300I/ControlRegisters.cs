using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    public class ControlRegisters
    {
        const int SIZE = 32;
        private ulong[] m_Registers;
        private Action<int, ulong>[] m_WriteMap;
        private Func<int, ulong>[] m_ReadMap;

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
    }
}
