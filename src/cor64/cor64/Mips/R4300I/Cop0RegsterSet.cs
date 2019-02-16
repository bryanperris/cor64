using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    public class Cop0RegsterSet : IWatchableRegister<int>
    {
        private ulong[] m_Registers;
        public event ValueChangedHandler<int> ValueChanged;

        public Cop0RegsterSet()
        {
            m_Registers = new ulong[31];
        }

        public ulong Read(int i)
        {
            return m_Registers[i];
        }

        public void Write(int i, ulong value)
        {
            m_Registers[i] = value;
            OnvalueChanged(i);
        }

        /* This interface is used by core instructions */
        public void WriteFromGpr(int index, ulong value)
        {
            m_Registers[index] = value;
            OnvalueChanged(index);
        }

        public void WriteFromInternal(int index, ulong value)
        {
            m_Registers[index] = value;
            OnvalueChanged(index);
        }

        protected virtual void OnvalueChanged(int index)
        {
            ValueChanged?.Invoke(index);
        }
    }
}
